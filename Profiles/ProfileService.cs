using Nest;
using Server.Database;
using Stormancer.Diagnostics;
using Stormancer.Server.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Profiles
{
    class ProfileService : IProfileService
    {
        private readonly IESClientFactory _factory;
        private readonly string _index;
        private readonly ILogger _logger;

        public ProfileService(IESClientFactory esClientFactory, IEnvironment environment, ILogger logger)
        {
            _factory = esClientFactory;

            _index = (string)(environment.Configuration.index);

            _logger = logger;
        }

        private Task<IElasticClient> GetClient()
        {
            return _factory.CreateClient(_index);
        }
        public async Task DeleteProfile(string profileId)
        {
            var client = await GetClient();

            var response = await client.DeleteAsync<PlayerProfile>(profileId);

            if (!response.IsValid)
            {
                throw new Exception($"Failed to delete profile {profileId}: {response.ServerError.Error}");
            }
        }

        public async Task<PlayerProfile> GetProfile(string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException("profileId");
            }
            var client = await GetClient();
            try
            {
                var response = await client.GetAsync<PlayerProfile>("profile-" + userId);


                if (!response.Found)
                {
                    return null;
                }
                if (!response.IsValid)
                {
                    throw new Exception($"Failed to get profile {userId}: {response.ServerError.Error}");
                }
                return response.Source;
            }
            catch (Exception ex)
            {
                throw new Exception($"an error occured while trying to get profile {userId}", ex);
            }

        }



        public async Task<PlayerProfile> UpdateProfile(PlayerProfile profile)
        {

            var client = await GetClient();

            var response = await client.IndexAsync(profile);

            if (!response.IsValid)
            {
                throw new Exception($"Failed to update profile {profile.Id}: '{response.ServerError.Error}'");
            }

            return profile;
        }

        public Task<PlayerProfile> CreateProfile(string userId, Action<PlayerProfile> initializer)
        {
            var profile = new PlayerProfile { Id = "profile-" + userId, PlayerId = userId };
            initializer(profile);
            return UpdateProfile(profile);
        }
    }
}
