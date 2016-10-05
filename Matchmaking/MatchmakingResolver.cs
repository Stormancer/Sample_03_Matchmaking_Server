using Battlefleet.Server.Matchmaking.Models;
using Server.Matchmaking.Dto;
using Stormancer.Matchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Diagnostics;
using Server.Users;
using Server.Profiles;

using Group = Stormancer.Matchmaking.Group;
using Server.Plugins.Steam;
using Server.Management;

namespace Sample.Server.Matchmaking
{
    public class MatchmakingResolver : IMatchmakingResolver
    {
        private readonly ILogger _logger;
        private readonly IUserSessions _sessions;
        private readonly IUserService _users;
        private readonly RulesService _rulesService;
        private readonly ISteamService _steamService;
        private readonly ManagementClientAccessor _management;

        public MatchmakingResolver(
            ILogger logger,
            IUserSessions sessions,
            IUserService users,
            RulesService rulesService,
            ISteamService steamService,
            ManagementClientAccessor management)
        {
            _logger = logger;
            _sessions = sessions;
            _users = users;
            _rulesService = rulesService;
            _steamService = steamService;
            _management = management;
        }

        private bool _useMatchmakerStub = false;
        private bool _useSteamMockup = false;

        #region IConfigurationRefresh
        public void Init(dynamic config)
        {
            ApplyConfig(config);
        }

        public void ConfigChanged(dynamic newConfig)
        {
            ApplyConfig(newConfig);
        }

        private void ApplyConfig(dynamic config)
        {
            var matchmakerConfig = config.matchmaking;

            var useStubNode = matchmakerConfig?.usestub;
            _useMatchmakerStub = (useStubNode != null) && ((bool)useStubNode);

            var useSteamMockupNode = config.steam?.usemockup;
            _useSteamMockup = (useSteamMockupNode != null) && ((bool)useSteamMockupNode);

            _logger.Log(LogLevel.Debug, "matchmaking.resolver", "Matchmaking resolver configured.", new { useStub = _useMatchmakerStub, useSteamMockup = _useSteamMockup });
        }
        #endregion

        public Task PrepareMatchResolution(MatchmakingResult matchmakingResult)
        {
            //_logger.Log(LogLevel.Trace, "matchmaking.resolver", "Preparing matchmaking resolution.", new { });

            if (_useMatchmakerStub)
            {
                return Task.FromResult(true); 
            }

            var result = new Dictionary<string, player>();


            foreach (var p in matchmakingResult.Matches.SelectMany(match => match.AllPlayers))
            {
                result.Add(p.UserId, new player { playerId = p.UserId, pseudo = ((PlayerProfile)p.Data).Name??"" });// Add custom player info in response here.
            }
            //if (!_useSteamMockup)
            //{
            //key: steamId, value: userId
            //var steamIdsDictionary = (await Task.WhenAll(matchmakingResult.Matches.SelectMany(match => match.AllPlayers).Select(async p => new { p.UserId, SteamId = await ExtractSteamId(p.UserId) }))).ToDictionary(r => r.SteamId, r => r.UserId);

            //var steamProfiles = await _steamService.GetPlayerSummaries(steamIdsDictionary.Keys);

            //foreach (var kvp in steamProfiles)
            //{
            //    var playerId = steamIdsDictionary[kvp.Key];
            //    result[playerId] = new player { playerId = playerId, steamId = kvp.Key, steamName = kvp.Value.personaname };
            //}
            //}
            //else
            //{
            //    foreach (var player in matchmakingResult.Matches.SelectMany(match => match.AllPlayers))
            //    {
            //        result[player.UserId] = new player { playerId = player.UserId }; 
            //    }
            //}

            foreach (var match in matchmakingResult.Matches)
            {
                match.CommonCustomData = result;
            }
            return Task.FromResult(true);
        }

        public async Task ResolveMatch(IMatchResolverContext matchCtx)
        {
            try
            {

                //_logger.Log(LogLevel.Trace, "matchmaking.resolver", "Resolving a successful match.", new { });


                var matchData = (MatchData)matchCtx.Match.CustomData;
                var response = new MatchmakingResponse();

                response.gameId = "game-" + matchCtx.Match.Id;

                response.team1.AddRange(GetPlayers(matchCtx.Match, matchCtx.Match.Teams[0]));
                response.team2.AddRange(GetPlayers(matchCtx.Match, matchCtx.Match.Teams[1]));

                //_logger.Log(LogLevel.Trace, "matchmaking.resolver", $"sending matchmaking response", response.ForLog());

                var managementClient = await _management.GetApplicationClient();

                var metadata = Newtonsoft.Json.Linq.JObject.FromObject(new { gameSession = new { userIds = matchCtx.Match.AllPlayers.Select(p => p.UserId).ToList() } });


                await managementClient.CreateScene(
                    response.gameId,
                    global::Server.App.GAMESESSION_SCENE_TYPE,
                    false,
                    metadata,
                    false);

                _logger.Log(LogLevel.Trace, "matchmaking resolver", $"Created scene for game {response.gameId}",
                    new {
                        id = response.gameId,
                        template = global::Server.App.GAMESESSION_SCENE_TYPE,
                        isPublic = false,
                        metadata,
                        isPersistent = false
                    });
                _logger.Log(LogLevel.Trace, "matchmaking.resolver", "Match found and resolved.", response);
                
                matchCtx.ResolutionAction = (writerCtx => writerCtx.WriteObjectToStream(response));



            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "matchmaking.resolver", "An exception occurred while resolving a successful match.", ex);
                throw;
            }

        }

        private int GetTeamCount(IEnumerable<Group> team) => team.Sum(g => g.Players.Count);

        private IEnumerable<player> GetPlayers(Match match, Team team)
            => team.Groups.SelectMany(g => g.Players).Select(p => ((Dictionary<string, player>)match.CommonCustomData)[p.UserId]);






    }
}
