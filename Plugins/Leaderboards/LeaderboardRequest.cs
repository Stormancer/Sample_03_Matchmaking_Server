using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Leaderboard
{
    public class LeaderboardQuery
    {
        [MessagePackMember(0)]
        public string StartId { get; set; }

        [MessagePackMember(1)]
        public List<ScoreFilter> ScoreFilters { get; set; }

        [MessagePackMember(2)]
        public List<FieldFilter> FieldFilters { get; set; }

    

        [MessagePackMember(3)]
        public int Count { get; set; }

        [MessagePackMember(4)]
        public int Skip { get; set; }

        [MessagePackMember(5)]
        public string Name { get; set; }

        [MessagePackMember(6)]
        public List<string> FriendsIds { get; set; } = new List<string>();
    }

    public class LeaderboardContinuationQuery : LeaderboardQuery
    {
        public LeaderboardContinuationQuery()
        {
        }

        internal LeaderboardContinuationQuery(LeaderboardQuery parent)
        {
            StartId = parent.StartId;
            ScoreFilters = parent.ScoreFilters;
            FieldFilters = parent.FieldFilters;
            FriendsIds = parent.FriendsIds;
            Count = parent.Count;
            Skip = parent.Skip;
            Name = parent.Name;
        }

        
        public bool IsPrevious { get; set; }
    }

    public class ScoreFilter
    {
        [MessagePackMember(0)]
        public ScoreFilterType Type { get; set; }

        [MessagePackMember(1)]
        public long Value { get; set; }
    }

    public class FieldFilter
    {
        [MessagePackMember(0)]
        public string Field { get; set; }
        [MessagePackMember(1)]
        public string Value { get; set; }
    }
    public enum ScoreFilterType
    {
        GreaterThanOrEqual = 0,
        GreaterThan = 1,
        LesserThanOrEqual = 2,
        LesserThan = 3
    }

    public class LeaderboardRanking<T>
    {
        [MessagePackMember(0)]
        public int Ranking { get; set; }
        [MessagePackMember(1)]
        public T Document { get; set; }
    }
    public class LeaderboardResult<T>
    {
        [MessagePackMember(0)]
        public List<LeaderboardRanking<T>> Results { get; set; } = new List<LeaderboardRanking<T>>();

        [MessagePackMember(1)]
        public string Next { get; set; } = "";

        [MessagePackMember(2)]
        public string Previous { get; set; } = "";
    }
}
