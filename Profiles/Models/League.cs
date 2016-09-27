using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Profiles.Models
{
    public class League
    {
        [MessagePackMember(0)]
        public int Id { get; set; }

        [MessagePackMember(1)]
        public int MinRank { get; set; }

        [MessagePackMember(2)]
        public int MaxRank { get; set; }
    }
}
