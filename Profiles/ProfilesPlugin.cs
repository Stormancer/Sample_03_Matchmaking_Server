using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Plugins;
using Stormancer.Server;
using Server.Database;
using Stormancer.Server.Components;
using Nest;
using Server.Users;
using Stormancer;
using Stormancer.Diagnostics;

namespace Server.Profiles
{
    class ProfilesPlugin : Stormancer.Plugins.IHostPlugin
    {
        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostDependenciesRegistration += (builder) =>
            {
                builder.Register<ProfileService>().As<IProfileService>();
               
                builder.Register<ProfilesUserEventHandler>().As<IUserEventHandler>();
            };


            ctx.HostStarted += (IHost host) =>
            {
                // Create PlayerProfile mapping

                var clientFactory = host.DependencyResolver.Resolve<IESClientFactory>();
                var environment = host.DependencyResolver.Resolve<IEnvironment>();

                var _ = Task.Run(async () =>
                {
                    var client = await clientFactory.CreateClient((string)environment.Configuration.index);
                    await client.MapAsync<PlayerProfile>(m =>
                            m.Properties(p =>
                                p.String(s =>
                                    s.Name(profile => profile.PlayerId)
                                    .Index(FieldIndexOption.NotAnalyzed)
                                )
                            )
                        );

                 
                  

                });
               
            };
        }
    }
}
