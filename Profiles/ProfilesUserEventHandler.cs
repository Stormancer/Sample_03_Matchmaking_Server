using Server.Database;
using Server.Users;
using Stormancer.Server.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Server.Plugins.Steam;

namespace Server.Profiles
{
    class ProfilesUserEventHandler : IUserEventHandler, IUserSessionEventHandler
    {
        private readonly IESClientFactory _client;
        private string _indexName;
        private readonly ISteamService _steam;
        private readonly IProfileService _profiles;
        public ProfilesUserEventHandler(IESClientFactory clientFactory,
            IEnvironment environment, 
            ISteamService steam,
            IProfileService profiles)
        {
            _client = clientFactory;
            _steam = steam;
            _profiles = profiles;
            _indexName = (string)(environment.Configuration.index);
        }

      

        public Nest.BulkDescriptor OnBuildMergeQuery(IEnumerable<User> enumerable, User mainUser, object data, Nest.BulkDescriptor desc)
        {
            var profiles = (List<PlayerProfile>)data;
            foreach (var profile in profiles)
            {
                desc = desc.Update<object>(u => u.Doc(new { PlayerId = mainUser.Id }).Id(profile.Id));
            }
            return desc;


        }

        public async Task<object> OnMergedUsers(IEnumerable<User> deletedUsers,User mergeResult)
        {

            var client = await _client.CreateClient(_indexName);

            var profiles = await client.SearchAsync<PlayerProfile>(desc => desc.Query(q => q.Terms(tq => tq.Terms(deletedUsers.Select(u => u.Id)).Field(p => p.PlayerId).MinimumShouldMatch(1))));

            return profiles.Documents.ToList();


        }

    

        public Task OnMergingUsers(IEnumerable<User> users)
        {
            return Task.FromResult(true);
        }



        public Task OnLoggedIn(IScenePeerClient client, User user, PlatformId pId)
        {

            return Task.FromResult(true);
            
        }

        public Task OnLoggedOut(long peerId, User user)
        {
            return Task.FromResult(true);
        }


    }
}
