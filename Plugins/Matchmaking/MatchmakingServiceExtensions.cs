using Stormancer.Core;
using Stormancer.Matchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public static class MatchmakingServiceExtensions
    {
        
        public static void AddMatchmaking(this ISceneHost scene, MatchmakingConfig config)
        {
            MatchmakingPlugin.Configs[config.Kind] = config;
            scene.Metadata[MatchmakingPlugin.METADATA_KEY] = config.Kind;
            
        }
    }
}
