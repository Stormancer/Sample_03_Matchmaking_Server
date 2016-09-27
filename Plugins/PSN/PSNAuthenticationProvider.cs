using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Core;

namespace Server.Users
{
    class PSNAuthenticationProvider : IAuthenticationProvider
    {
        public void AddMetadata(Dictionary<string, string> result)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> Authenticate(Dictionary<string, string> authenticationCtx, IUserService userService)
        {
            throw new NotImplementedException();
        }

        public void Initialize(ISceneHost scene)
        {
            throw new NotImplementedException();
        }
    }
}
