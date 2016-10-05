using Newtonsoft.Json.Linq;
using Server.Plugins.Configuration;
using Server.Users;
using Stormancer;
using Stormancer.Core;
using Stormancer.Diagnostics;
using Stormancer.Server.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server.Plugins.GameSession
{
    public enum ServerStatus
    {
        WaitingPlayers,
        Starting,
        Started,
        Shutdown
    }
    internal class GameSessionService : IGameSessionService
    {
        private readonly IUserSessions _sessions;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ISceneHost _scene;
        private readonly IEnvironment _environment;
        private readonly IDelegatedTransports _pools;
        private readonly IEnumerable<IGameSessionEventHandler> _eventHandlers;

        private GameSessionConfiguration _config;

        private IDisposable _portLease;

        private System.Diagnostics.Process _gameServerProcess;

        private class Client
        {
            public IScenePeerClient Peer { get; set; }

            public Stream ResultData { get; set; }

            public bool Disconnected { get; set; }
        }
        private ConcurrentDictionary<string, Client> _clients = new ConcurrentDictionary<string, Client>();
        private ServerStatus _status;
         
        private string _ip;
        private ushort _port;

        public GameSessionService(
            ISceneHost scene,
            IUserSessions sessions,
            IConfiguration configuration,
            IEnvironment environment,
            IDelegatedTransports pools,
            ILogger logger,
            IEnumerable<IGameSessionEventHandler> eventHandlers)
        {
            _scene = scene;
            _sessions = sessions;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _pools = pools;

            _eventHandlers = eventHandlers;

            scene.Shuttingdown.Add(args =>
            {
                _logger.Log(LogLevel.Trace, "gameserver", $"Shutting down gamesession scene {_scene.Id}.", new { _scene.Id, Port = _port });

                try
                {
                    if (_gameServerProcess != null && !_gameServerProcess.HasExited)
                    {
                        _gameServerProcess.Close();
                        _gameServerProcess = null;
                    }
                }
                catch { }
                finally
                {
                    _portLease?.Dispose();
                }
                _logger.Log(LogLevel.Trace, "gameserver", $"gamesession scene {_scene.Id} shut down.", new
                {
                    _scene.Id,
                    Port = _port
                });

                return Task.FromResult(true);
            });
            scene.Connecting.Add(this.PeerConnecting);
            scene.Disconnected.Add((args) => this.PeerDisconnecting(args.Peer));
        }
        public void SetConfiguration(dynamic metadata)
        {
            if (metadata.gameSession != null)
            {
                _config = ((JObject)metadata.gameSession).ToObject<GameSessionConfiguration>();
            }
        }


        public async Task PeerConnecting(IScenePeerClient peer)
        {
            if (peer == null)
            {
                throw new ArgumentNullException("peer");
            }
            var user = await _sessions.GetUser(peer);

            if (user == null)
            {
                throw new ClientException("You are not authenticated.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException("Game session plugin configuration missing in scene instance metadata. Please check the scene creation process.");
            }
            if (!_config.userIds.Contains(user.Id))
            {
                throw new ClientException("You are not authorized to join this game.");
            }

            if (!_clients.TryAdd(user.Id, new Client { Peer = peer }))
            {
                throw new ClientException("Failed to add player to the game session.");
            }

            _scene.Broadcast("player.joined", user.Id);
            foreach (var uId in _clients.Keys)
            {
                if (uId != user.Id)
                {
                    peer.Send("player.joined", uId);
                }
            }
            if (_status == ServerStatus.Started)
            {
                peer.Send("server.started", new GameServerStartMessage { Ip = _ip, Port = _port });
            }
            var _ = TryStart();

        }

        private AsyncLock _lock = new AsyncLock();

        public async Task TryStart()
        {
            using (await _lock.LockAsync())
            {
                if (_config.userIds.All(id => _clients.Keys.Contains(id)) && _status == ServerStatus.WaitingPlayers)
                {
                    _status = ServerStatus.Starting;
                    await Start();
                    var ctx = new GameSessionStartedCtx(_clients.Select(kvp => new Player(kvp.Value.Peer, kvp.Key)));
                    await _eventHandlers.RunEventHandler(eh => eh.GameSessionStarted(ctx), ex => _logger.Log(LogLevel.Error, "gameSession", "An error occured while running gameSession.Started event handlers", ex));
                }
            }
        }

        private async Task Start()
        {
            var serverEnabled = ((JToken)_configuration?.Settings?.gameServer) != null;
            var path = (string)_configuration.Settings?.gameServer?.executable;
            var verbose = ((bool?)_configuration.Settings?.gameServer?.verbose) ?? false;
            var log = ((bool?)_configuration.Settings?.gameServer.log) ?? false;

            if (!serverEnabled)
            {
                _status = ServerStatus.Started;
                return;
            }

            try
            {

                if (path == null)
                {
                    throw new InvalidOperationException("Missing 'gameServer.executable' configuration value");
                }

                if (path == "dummy")
                {
                    _logger.Log(LogLevel.Trace, "gameserver", "Using dummy: no executable server available.", new { });
                    try
                    {
                        await LeaseServerPort();

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        _status = ServerStatus.Started;
                        var gameStartMessage = new GameServerStartMessage { Ip = _ip, Port = _port };
                        _logger.Log(LogLevel.Trace, "gameserver", "Dummy server started, sending server.started message to connected players.", gameStartMessage);
                        _scene.Broadcast("server.started", gameStartMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, "gameserver", "An error occurred while trying to lease a port", ex);
                        throw;
                    }

                    return;
                }

                var prc = new System.Diagnostics.Process();

                await LeaseServerPort();

                prc.StartInfo.Arguments = $"-port={_port} {(log ? "-log" : "")}";
                prc.StartInfo.FileName = path;
                prc.StartInfo.CreateNoWindow = true;
                prc.StartInfo.UseShellExecute = false;
                prc.StartInfo.RedirectStandardOutput = true;
                prc.StartInfo.RedirectStandardError = true;

                prc.OutputDataReceived += (sender, args) =>
                {
                    if (verbose)
                    {
                        _logger.Log(LogLevel.Trace, "gameserver", "Received data output from Battlecrew server.", new { args.Data });
                    }
                    if (args.Data?.ToLowerInvariant().Contains("server is ready") == true)
                    {
                        _logger.Log(LogLevel.Trace, "gameserver", "Server responded as ready.", new { Port = _port });
                        _scene.Broadcast("server.started", new GameServerStartMessage { Ip = _ip, Port = _port });
                        _status = ServerStatus.Started;
                    }

                };
                prc.ErrorDataReceived += (sender, args) =>
                  {
                      _logger.Error("gameserver", $"An error occured while trying to start the game server : '{args.Data}'");
                  };

                prc.Exited += (sender, args) =>
                {
                    _status = ServerStatus.Shutdown;
                    foreach (var client in _clients.Values)
                    {
                        client.Peer.Disconnect("Game server stopped");
                    }
                };

                _gameServerProcess = prc;

                prc.Start();
                prc.BeginErrorReadLine();
                prc.BeginOutputReadLine();

                await Task.Delay(TimeSpan.FromSeconds(3));
                _logger.Log(LogLevel.Trace, "gameserver", "Waited 3 seconds for server to start, sending server.started to players.", new { Port = _port });
                _scene.Broadcast("server.started", new GameServerStartMessage { Ip = _ip, Port = _port });
                _status = ServerStatus.Started;
            }
            catch (InvalidOperationException ex)
            {
                _status = ServerStatus.Shutdown;
                _logger.Log(LogLevel.Error, "gameserver", "Failed to start server.", ex);
                foreach (var client in _clients.Values)
                {
                    await client.Peer.Disconnect("Game server stopped");
                }
            }
        }

        private async Task LeaseServerPort()
        {


            var lease = await _pools.AcquirePort((string)_configuration.Settings?.gameServer?.transport ?? "public1");
            if (!lease.Success)
            {

                throw new InvalidOperationException("Unable to acquire port for the server");
            }
            _portLease = lease;
            _port = lease.Port;
            _ip = lease.PublicIp;

        }

        public async Task PeerDisconnecting(IScenePeerClient peer)
        {
            if (peer == null)
            {
                throw new ArgumentNullException("peer");
            }
            var user = await _sessions.GetUser(peer);

            if (user != null)
            {


                var client = _clients[user.Id];
                client.Peer = null;
                client.Disconnected = true;

                _scene.Broadcast("player.left", user.Id);
                EvaluateGameComplete();
            }

            if (!_scene.RemotePeers.Any())
            {
                if (_gameServerProcess != null && !_gameServerProcess.HasExited)
                {
                    _logger.Log(LogLevel.Trace, "gameserver", $"Closing down game server for scene {_scene.Id}.", new { _scene.Id, Port = _port });
                    _gameServerProcess.Close();
                    _gameServerProcess = null;

                }
                _portLease?.Dispose();

                _logger.Log(LogLevel.Trace, "gameserver", $"Game server for scene {_scene.Id} shut down.", new { _scene.Id, Port = _port });
            }

        }

        public async Task PostResults(Stream inputStream, IScenePeerClient remotePeer)
        {
            if(this._status!= ServerStatus.Started)
            {
                throw new ClientException("Unable to post result before game session start.");
            }
            var user = await _sessions.GetUser(remotePeer);

            EvaluateGameComplete();
        }

        private void EvaluateGameComplete()
        {
            if(_clients.Values.All(c=>c.ResultData !=null))//All remaining clients sent their data!
            {
                var ctx = new GameSessionCompleteCtx(_clients.Select(kvp => new GameSessionResult(kvp.Key,kvp.Value.Peer,kvp.Value.ResultData)));


            }
        }
    }
}
