using Server.Plugins.API;
using Server.Plugins.Database;
using Server.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Plugins;
using Stormancer.Diagnostics;
using Server.Database;
using Stormancer.Server.Components;

namespace Server.Plugins.Leaderboard
{


    class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboard;

        public LeaderboardController(ILeaderboardService leaderboard)
        {
            _leaderboard = leaderboard;
        }

        public async Task Query(RequestContext<IScenePeerClient> ctx)
        {
            var rq = ctx.ReadObject<LeaderboardQuery>();
            if (rq.Count <= 0)
            {
                rq.Count = 10;
            }
            var r = await _leaderboard.Query(rq);
            ctx.SendValue(r);
 
        }

        public async Task Cursor(RequestContext<IScenePeerClient> ctx)
        {
            var cursor = ctx.ReadObject<string>();
            var result = await _leaderboard.QueryCursor(cursor);
            ctx.SendValue(result);
        }
        
    }


}
