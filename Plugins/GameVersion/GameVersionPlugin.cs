using System;
using Stormancer.Plugins;
using Stormancer.Core;
using Stormancer;
using Server.Plugins.Configuration;
using System.Threading.Tasks;
using Stormancer.Server.Components;

namespace Server.Plugins.GameVersion
{
    internal class GameVersionPlugin : IHostPlugin
    {
        internal const string METADATA_KEY = "stormancer.gameVersion";

        public void Build(HostPluginBuildContext ctx)
        {


            ctx.SceneCreated += (ISceneHost scene) =>
            {
                if (scene.Metadata.ContainsKey(METADATA_KEY))
                {
                    var prefix = scene.Metadata[METADATA_KEY];


                    var config = scene.DependencyResolver.Resolve<IConfiguration>();
                    var environment = scene.DependencyResolver.Resolve<IEnvironment>();
                    environment.ActiveDeploymentChanged += (sender, v) =>
                    {
                        if(!environment.IsActive)
                        {
                            scene.Broadcast("serverVersion.update", v.ActiveDeploymentId);
                        }
                    };
                    string currentGameVersion = GetVersion(config.Settings, prefix);

                    config.SettingsChanged += (sender, v) =>
                    {
                        var newGameVersion = GetVersion(v, prefix);
                        if (newGameVersion != currentGameVersion)
                        {
                            currentGameVersion = newGameVersion;
                            scene.Broadcast("gameVersion.update", currentGameVersion);
                        }
                    };

                    scene.Connected.Add(p =>
                    {
                        p.Send("gameVersion.update", currentGameVersion);
                        return Task.FromResult(true);
                    });

                }
            };
        }

        private string GetVersion(dynamic configuration, string prefix)
        {
            try
            {
                dynamic gameVersion = null;
                if (prefix == "default")
                {
                    gameVersion = configuration.gameVersion;
                }
                else
                {
                    if (configuration[prefix] != null)
                    {
                        gameVersion = configuration[prefix].gameVersion;
                    }
                }

                if (gameVersion != null)
                {
                    try
                    {
                        return (string)gameVersion;
                    }
                    catch (Exception) { }
                }

            }
            catch(Exception)
            {
                
            }
            return "NA";
        }
    }


}