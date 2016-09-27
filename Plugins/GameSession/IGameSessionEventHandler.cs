using Stormancer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.GameSession
{
    public interface IGameSessionEventHandler
    {
        Task GameSessionStarted(GameSessionStartedCtx ctx);

        Task GameSessionCompleted(GameSessionCompleteCtx ctx);
    }

    public class GameSessionStartedCtx
    {
        public GameSessionStartedCtx(IEnumerable<Player> peers)
        {
            Peers = peers;
        }
        public IEnumerable<Player> Peers { get; }
    }

    public class Player
    {
        public Player(IScenePeerClient client, string uid)
        {
            UserId = uid;
            Peer = client;
        }
        public IScenePeerClient Peer { get; }

        public string UserId { get; }
    }

    public class GameSessionCompleteCtx
    {
        public GameSessionCompleteCtx(IEnumerable<GameSessionResult> results)
        {
            Results = results;
        }

        public IEnumerable<GameSessionResult> Results { get; }
    }

    public class GameSessionResult
    {
        public GameSessionResult(string userId,IScenePeerClient client, Stream data)
        {
            Peer = client;
            Data = data;
            UserId = userId;
        }
        public IScenePeerClient Peer { get; }

        public string UserId { get;  }

        public Stream Data { get; }

        
    }

}
