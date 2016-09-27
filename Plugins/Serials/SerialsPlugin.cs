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
using Server.Plugins.AdminApi;
using System.Web.Http.Controllers;

namespace Server.Plugins.Serials
{
    class SerialsPlugin : IHostPlugin
    {
        internal const string METADATA_KEY = "stormancer.serials";

        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostDependenciesRegistration += (IDependencyBuilder builder) =>
              {
                  builder.Register<SerialsService>().As<ISerialsService>();
              
                  builder.Register<AdminWebApiConfig>().As<IAdminWebApiConfig>();
                  builder.Register<SerialsController>().InstancePerRequest();
                  builder.Register<SerialsAdminController>();
                  
              };

            ctx.SceneCreated += (ISceneHost scene) =>
             {
                 if (scene.Metadata.ContainsKey(METADATA_KEY))
                 {
                     scene.AddController<SerialsController>();
                 }
             };
        }
    }
}
