using Stormancer;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Plugins.API;
using Stormancer.Diagnostics;
using System.Runtime.Serialization;
using Server.Users;

namespace Server.Profiles
{
    class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ILogger _logger;
        private readonly IUserService _userService;
      
        private readonly IUserSessions _sessions;
        public ProfilesController(IProfileService profileService, IUserService userService, ILogger logger, IUserSessions sessions)
        {
            _profileService = profileService;
            _logger = logger;
            _userService = userService;
        
            _sessions = sessions;
        }

      

        private static void EnsureUserOwnsProfile(string userId, PlayerProfile profile, string errorMessage)
        {
            if (profile.PlayerId != userId)
            {
                throw new InvalidOperationException(errorMessage.With(userId, profile.Id));
            }
        }

        public Task Delete(RequestContext<IScenePeerClient> ctx) =>
            ControllerHelper.ToActionWithUserData<string, string>(Delete)(ctx);

        private async Task Delete(string userId, string profileId)
        {
            var profile = await this._profileService.GetProfile(profileId);

            if (profile == null)
            {
                throw new InvalidOperationException($"Profile with id {profileId} does not exist or is not indexed yet.");
            }

            EnsureUserOwnsProfile(userId, profile, "User {0} cannot delete a profile owned by user {1}.");

            await _profileService.DeleteProfile(profileId);
        }




        public async Task GetSingle(RequestContext<IScenePeerClient> ctx)
        {
            var userId = ctx.RemotePeer.GetUserData<string>();
            var profileId = ctx.ReadObject<string>();
            if (string.IsNullOrEmpty(profileId))
            {
                throw new ArgumentNullException($"profileId is null. userId={userId}");
            }
            var profile = await this._profileService.GetProfile(profileId);

            if (profile == null)
            {
                profile = await CreatePlayerProfile(ctx.RemotePeer);
            }

            if (profile != null)
            {
                EnsureUserOwnsProfile(userId, profile, "User {0} cannot retrieve a profile owned by user {1}.");
            }
            //_logger.Log(LogLevel.Debug, "profiles", "Get single ", new { metadata = ctx.RemotePeer.Metadata });
            if (!ctx.RemotePeer.Metadata.ContainsKey("bfg.profiles.compression"))
            {
                ctx.SendValue(profile);
            }
            else
            {

                ctx.SendValue(s =>
                {
                    using (var lzStream = new LZ4.LZ4Stream(s, LZ4.LZ4StreamMode.Compress, LZ4.LZ4StreamFlags.HighCompression))
                    {
                        ctx.RemotePeer.Serializer().Serialize(profile, lzStream);
                    }
                });
            }
        }

        private async Task<PlayerProfile> CreatePlayerProfile(IScenePeerClient peer)
        {
            var profile = new PlayerProfile();
            var user = await _sessions.GetUser(peer);

            profile.Id = "profile-" + user.Id;
            profile.Name = (string)user.UserData["pseudo"];

            profile.PlayerId = user.Id;
            profile.GMMR = 0;
            profile.RMMR = 0;

            await _profileService.UpdateProfile(profile);
            return profile;
        }
     
        protected internal override async Task<bool> HandleException(ApiExceptionContext ctx)
        {
            try
            {
                var peer = ctx.IsRpc ? ctx.Request.RemotePeer : ctx.Packet.Connection;

                var userId = peer.GetUserData<string>();

                var user = await _userService.GetUser(userId);

                var steamId = (string)(user.UserData["steamid"]);

                var stream = ctx.IsRpc ? ctx.Request.InputStream : ctx.Packet.Stream;
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                var buffer = new byte[stream.Length];

                stream.Read(buffer, 0, buffer.Length);

                _logger.Log(LogLevel.Error, ctx.Route, $"An error occurred while execution route '{ctx.Route}'", new { receivedBytes = Convert.ToBase64String(buffer), userId, steamId, route = ctx.Route });

            }
            catch (Exception)
            {
            }

            return false;
        }

    }
}
