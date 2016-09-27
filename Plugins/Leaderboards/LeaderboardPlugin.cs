using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Plugins.API;
using Stormancer.Server;
using Server.Users;

namespace Server.Plugins.Leaderboard
{
    class LeaderboardPlugin : IHostPlugin
    {
        internal const string METADATA_KEY = "stormancer.leaderboard";

        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostDependenciesRegistration += (IDependencyBuilder builder) =>
              {
                  builder.Register<LeaderboardService>().As<ILeaderboardService>();
                  builder.Register<LeaderboardController>().InstancePerRequest();
                  
              };

            ctx.SceneCreated += (ISceneHost scene) =>
             {
                 if (scene.Metadata.ContainsKey(METADATA_KEY))
                 {
                     scene.AddController<LeaderboardController>();
                 }
             };
        }
    }
}
