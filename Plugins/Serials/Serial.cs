using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Serials
{
    public class Serial
    {
        
        public string Id { get; set; }
        public string Value { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public string ClaimedBy { get; set; }
        public DateTime ClaimedOn { get; set; }

        public string Comment { get; set; }

        public bool IsValid { get; set; }
    }

    public class SerialDto
    {
        [MessagePackMember(0)]
        public string Id { get; set; }
        [MessagePackMember(1)]
        public string Value { get; set; }
    }
}
