using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Profiles
{
    public interface IProfileService
    {
        Task<PlayerProfile> GetProfile(string userId);

        Task DeleteProfile(string profileId);

        Task<PlayerProfile> UpdateProfile(PlayerProfile profile);

        Task<PlayerProfile> CreateProfile(string userId, Action<PlayerProfile> initializer);

    }
}
