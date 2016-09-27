using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Leaderboard
{
    public interface ILeaderboardService
    {
        Task<LeaderboardResult<ScoreRecord>> Query(LeaderboardQuery query);

        Task<LeaderboardResult<ScoreRecord>> QueryCursor(string cursor);

        Task UpdateScore(ScoreRecord score, string leaderboard);

        Task RemoveLeaderboardEntry(string leaderboardName, string entryId);

        Task<ScoreRecord> GetScore(string id, string leaderboardName);
        Task<long> GetRanking(ScoreRecord score, LeaderboardQuery filters, string leaderboardName);
        Task<long> GetTotal(LeaderboardQuery filters, string leaderboardName);


        /// <summary>
        /// Creates an elasticsearch client targeting a leaderboard's index.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<Nest.IElasticClient> CreateClient(string name);
    }
}
