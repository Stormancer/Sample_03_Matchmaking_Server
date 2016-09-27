using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Profiles
{
    public class PlayerProfile
    {
     
        
        [MessagePackMember(0)]
        public string Id { get; set; }

       

        [MessagePackMember(1)]
        public string PlayerId { get; set; }

       
        /// <summary>
        /// MMR Global (non ranked)
        /// </summary>
        [MessagePackMember(2)]
        public int GMMR { get; set; }

        /// <summary>
        /// MMR ranked
        /// </summary>
        [MessagePackMember(3)]
        public int RMMR { get; set; }

        public string Name { get; set; }
    }

}
