using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Matchmaking.Dto
{
    public class MatchmakingResponse
    {


        /// <summary>
        /// The unique id of the game.
        /// </summary>
        [MessagePackMember(0)]
        public string gameId { get; set; } = "";

        /// <summary>
        /// The list of steam ids for players in the first team.
        /// </summary>
        [MessagePackMember(1)]
        public List<player> team1 { get; set; } = new List<player>();

        /// <summary>
        /// The list of steam ids for players in the second team.
        /// </summary>
        [MessagePackMember(2)]
        public List<player> team2 { get; set; } = new List<player>();

        /// <summary>
        /// The list of optional parameters satisfied by the match. 
        /// </summary>
        [MessagePackMember(3)]
        public List<string> optionalParameters { get; set; } = new List<string>();



        public MatchmakingResponseLoggable ForLog()
            => new MatchmakingResponseLoggable
            {
              
               
                gameId = this.gameId,
                optionalParameters = this.optionalParameters,
                team1 = this.team1.Select(ForLog).ToList(),
                team2 = this.team2.Select(ForLog).ToList()
            };

        private playerLoggable ForLog(player player) => new playerLoggable { playerId = player.playerId };

    }

    public class player
    {
        [MessagePackMember(0)]
        public string playerId { get; set; } = "";

        [MessagePackMember(1)]
        public string pseudo { get; set; } = "";
    }

    public class MatchmakingResponseLoggable
    {
        public string alliance1 { get; set; } = "";

        public string alliance2 { get; set; } = "";

        public string gameHost { get; set; }

        public string gameId { get; set; }

        public List<playerLoggable> team1 { get; set; } = new List<playerLoggable>();

        public List<playerLoggable> team2 { get; set; } = new List<playerLoggable>();

        public List<string> optionalParameters { get; set; } = new List<string>();

    }

    public class playerLoggable
    {
        public string steamId { get; set; }

        public string playerId { get; set; }
    }

}
