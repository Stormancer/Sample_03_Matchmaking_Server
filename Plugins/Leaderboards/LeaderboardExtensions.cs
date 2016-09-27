using Server.Plugins.Leaderboard;
using Server.Plugins.Nat;
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public static class LeaderboardExtensions
    {
        public static void AddLeaderboard(this ISceneHost scene)
        {
            scene.Metadata[LeaderboardPlugin.METADATA_KEY] = "enabled";
        }
    }
}
