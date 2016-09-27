using Stormancer.Matchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlefleet.Server.Matchmaking.Models
{
    public class MatchData
    {
      

        public string HostUserId { get; set; }

     

        public List<string> OptionalParameters { get; } = new List<string>();
    }
}
