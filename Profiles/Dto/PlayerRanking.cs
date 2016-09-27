using Server.Profiles.Models;
using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Profiles.Dto
{
    public class PlayerRanking
    {
        [MessagePackMember(0)]
        public string ProfileId { get; set; }

        [MessagePackMember(1)]
        public int League { get; set; }

        [MessagePackMember(2)]
        public int RankingInLeague { get; set; }

        [MessagePackMember(3)]
        public int TotalPlayersInLeague { get; set; }

        [MessagePackMember(4)]
        public List<League> Leagues { get; set; }

    }
}
