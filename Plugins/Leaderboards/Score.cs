using MsgPack.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Leaderboard
{
    public class ScoreRecordBase
    {
        [MessagePackMember(0)]
        public string Id { get; set; }

        [MessagePackMember(1)]
        public string PlayerId { get; set; }

        [MessagePackMember(2)]
        public int Score { get; set; }

        [MessagePackMember(3)]
        public DateTime CreatedOn { get; set; }

    }
    public class ScoreRecord:ScoreRecordBase
    {

        [MessagePackMember(4)]
        public JObject Document { get; set; }

    }

    public class ScoreRecord<T>:ScoreRecordBase
    {
        public ScoreRecord(ScoreRecord r)
        {
            Id = r.Id;
            PlayerId = r.PlayerId;
            Score = r.Score;
            CreatedOn = r.CreatedOn;

        }
        public T Document { get; set; }
    }
}
