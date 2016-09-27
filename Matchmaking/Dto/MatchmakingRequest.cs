using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Matchmaking.Dto
{
    public class MatchmakingRequest
    {
        /// <summary>
        /// membres du groupe présentés sous la forme [userId]=>[profileId]
        /// </summary>
        [MessagePackMember(0)]
        public string gameMode { get; set; }
        
      
    }
}
