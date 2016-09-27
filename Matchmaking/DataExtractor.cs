using Battlefleet.Server.Matchmaking.Models;
using Stormancer.Matchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Plugins;
using Server.Matchmaking.Dto;
using Server.Users;
using Server.Profiles;

namespace Sample.Server.Matchmaking
{
    public class DataExtractor : IMatchmakingDataExtractor
    {

        private readonly IUserSessions _sessions;
        private readonly IProfileService _profileService;

        public DataExtractor(IUserSessions sessions, IProfileService profileService)
        {
            _sessions = sessions;
            _profileService = profileService;
        }

        #region IConfigurationRefresh
        public void Init(dynamic config)
        {
        }

        public void ConfigChanged(dynamic newConfig)
        {
        }
        #endregion

        public async Task<bool> ExtractData(string provider, RequestContext<IScenePeerClient> request, Stormancer.Matchmaking.Group group)
        {
            if (provider != "matchmaking-sample")
            {
                return false;
            }

            ///Get leader
            var user = await _sessions.GetUser(request.RemotePeer);

            if (user == null)
            {
                throw new InvalidOperationException("User is not logged in.");
            }

            //var userId = user.Id;

            var requestData = request.ReadObject<MatchmakingRequest>();

            //if (requestData.profileIds.Count > 1)
            //{
            //    throw new ArgumentException($"group size must be 1.");
            //}

            group.GroupData = new MatchmakingGroupData(requestData);

            //foreach (var member in requestData.profileIds)
            //{
            //    var userId = member.Key;

            //    var peer = await _sessions.GetPeer(userId);
            //    if(peer == null)
            //    {
            //        throw new InvalidOperationException("An user in the group is not logged in.");
            //    }

            //    var profile = await _profileService.GetProfile(member.Value);

            //    if(profile == null)
            //    {
            //        throw new InvalidOperationException($"Couldn't find profile '{member.Value}'");
            //    }

            //    if(profile.PlayerId != userId)
            //    {
            //        throw new InvalidOperationException($"Profile '{member.Value}' is not associated with player '{userId}'");
            //    }

            //    group.Players.Add(new Stormancer.Matchmaking.Player(userId) { Data = profile  });

            //}
            var profile = await _profileService.GetProfile(user.Id);
            if(profile == null)
            {

                profile = await _profileService.CreateProfile(user.Id, p => {
                    p.Name = (string)user.UserData["pseudo"];
                });
            }
            group.Players.Add(new Stormancer.Matchmaking.Player(user.Id) { Data = profile });
            
            if (group.Players.Count == 0)
            {
                throw new ArgumentException("A group must contain at least one player.");
            }

            return true;
        }

    }
}
