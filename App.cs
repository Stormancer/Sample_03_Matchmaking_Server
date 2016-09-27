using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Users;
using Server.Plugins.API;
using Stormancer.Diagnostics;
using Stormancer.Configuration;
using Stormancer.Matchmaking;
using Sample.Server.Matchmaking;
using Newtonsoft.Json.Linq;
using Server.Plugins.Steam;
using Server.Profiles;

namespace Server
{
    public class App
    {
        public const string GAMESESSION_SCENE_TYPE = "gameSession";
        public void Run(IAppBuilder builder)
        {
            builder.AddPlugin(new ConfigurationManagerPlugin());

            builder.AddPlugin(new SteamPlugin());

            
            
            //Add matchmaking plugin
            var fastMatchMakingConfig = new MatchmakingConfig("fast", b =>
            {
                b.Register<DataExtractor>().As<IMatchmakingDataExtractor>();
                b.Register<Matchmaker>().As<IMatchmaker>();
                b.Register<MatchmakingResolver>().As<IMatchmakingResolver>();
                b.Register<RulesService>(resolver => new RulesService(resolver.Resolve<ILogger>(), p => p.GMMR));
            });
            var rankedMatchMakingConfig = new MatchmakingConfig("ranked", b =>
            {
                b.Register<DataExtractor>().As<IMatchmakingDataExtractor>();
                b.Register<Matchmaker>().As<IMatchmaker>();
                b.Register<MatchmakingResolver>().As<IMatchmakingResolver>();
                b.Register<RulesService>(resolver => new RulesService(resolver.Resolve<ILogger>(), p => p.RMMR));
            });

            builder.SceneTemplate("main", scene =>
            {
                scene.AddController<ProfilesController>();
                scene.Metadata["stormancer.profiles"] = "enabled";
                scene.AddPeerMessaging();
                //scene.AddNatPunchthrough();
                scene.AddLogsServer();
                scene.AddGameVersion();
                scene.AddLeaderboard();
                scene.AddAnalytics();

            });

            builder.SceneTemplate(GAMESESSION_SCENE_TYPE, scene =>
            {
                scene.AddGameSession();
                scene.AddChat();
            });

            builder.SceneTemplate("matchmaker-fast", scene =>
             {
                 scene.AddMatchmaking(fastMatchMakingConfig);
             });
            builder.SceneTemplate("matchmaker-ranked", scene =>
            {
                scene.AddMatchmaking(rankedMatchMakingConfig);
                
            });

           
            builder.AdminPlugin("users").Name("Users");
            var userConfig = new Users.UserManagementConfig() { SceneIdRedirect = "services" /*Constants.MATCHMAKER_NAME*/ };
            //userConfig.AuthenticationProviders.Add(new LoginPasswordAuthenticationProvider());
            userConfig.AuthenticationProviders.Add(new SteamAuthenticationProvider());
            userConfig.AuthenticationProviders.Add(new AdminImpersonationAuthenticationProvider());

            builder.AddPlugin(new UsersManagementPlugin(userConfig));

            builder.AddPlugin(new ProfilesPlugin());
        }
    }
}
