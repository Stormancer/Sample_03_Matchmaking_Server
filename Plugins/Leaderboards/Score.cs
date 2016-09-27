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
        public string Id { get; set; }
        public string PlayerId { get; set; }

        public int Score { get; set; }

        public DateTime CreatedOn { get; set; }

    }
    public class ScoreRecord:ScoreRecordBase
    {
       

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
