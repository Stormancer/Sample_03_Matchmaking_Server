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
using Stormancer.Diagnostics;

namespace Server.Plugins.Analytics
{
    class AnalyticsPlugin : IHostPlugin
    {
        internal const string METADATA_KEY = "stormancer.analytics";

        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostDependenciesRegistration += (IDependencyBuilder builder) =>
              {
                  builder.Register<Plugins.Nat.NatUserSessionEventHandler>().As<IUserSessionEventHandler>();
                  builder.Register<AnalyticsController>().InstancePerRequest();
                  builder.Register<AnalyticsService>().As<IAnalyticsService>().SingleInstance();
              };
            ctx.HostStarted += (IHost host) =>
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(1000 * 60);
                            var analyticsService = host.DependencyResolver.Resolve<IAnalyticsService>();
                            await analyticsService.Flush();
                        }
                        catch(Exception ex)
                        {
                            host.DependencyResolver.Resolve<ILogger>().Log(LogLevel.Error, "analytics", "failed to push analytics", ex);
                        }
                    }
                    
                });
            };
            ctx.SceneCreated += (ISceneHost scene) =>
             {
                 if (scene.Metadata.ContainsKey(METADATA_KEY))
                 {
                     scene.AddController<AnalyticsController>();
                 }
             };
        }
    }
}
