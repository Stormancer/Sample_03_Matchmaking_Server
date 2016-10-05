using Stormancer.Core;
using Stormancer.Diagnostics;
using Stormancer.Plugins;
using Stormancer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Matchmaking
{
    public class MatchmakingPlugin : IHostPlugin
    {
        public const string METADATA_KEY = "stormancer.plugins.matchmaking";

        internal static Dictionary<string, MatchmakingConfig> Configs = new Dictionary<string, MatchmakingConfig>();

        public void Build(HostPluginBuildContext ctx)
        {
            //ctx.HostStarting += HostStarting;


            ctx.SceneDependenciesRegistration += SceneDependenciesRegistration;
            ctx.SceneCreated += SceneCreated;
        }


        private void SceneDependenciesRegistration(IDependencyBuilder builder, ISceneHost scene)
        {
            string kind;
            if (scene.Metadata.TryGetValue(METADATA_KEY, out kind))
            {
                MatchmakingConfig config;
                if (Configs.TryGetValue(kind, out config))
                {
                    config.RegisterDependencies(builder);
                }
            }
            builder.Register<MatchmakingService>().As<IMatchmakingService>().InstancePerScene();
        }


        private void SceneCreated(ISceneHost scene)
        {
            string kind;
            if (scene.Metadata.TryGetValue(METADATA_KEY, out kind))
            {

                var logger = scene.DependencyResolver.Resolve<ILogger>();
                try
                {
                    var config = Configs[kind];


                    var matchmakingService = scene.DependencyResolver.Resolve<IMatchmakingService>();
                    matchmakingService.Init(scene);

                    scene.Disconnected.Add(args => matchmakingService.CancelMatch(args.Peer));
                    scene.AddProcedure("match.find", matchmakingService.FindMatch);
                    scene.AddRoute("match.ready.resolve", matchmakingService.ResolveReadyRequest, _ => _);
                    scene.AddRoute("match.cancel", matchmakingService.CancelMatch, _ => _);

                    //Start matchmaking
                    scene.RunTask(matchmakingService.Run);

                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "plugins.matchmaking", $"An exception occured when creating scene {scene.Id}.", ex);
                    throw;
                }
            }
        }


    }
}
