using Server.Matchmaking.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlefleet.Server.Matchmaking.Models
{

    public class MatchmakingGroupData
    {
        public MatchmakingGroupData(MatchmakingRequest request)
        {
            Request = request;
        }
        public int PastMatchmakingPasses { get; set; }
        public MatchmakingRequest Request { get; }

    }
}
