using Server.Plugins.API;
using Server.Plugins.Database;
using Server.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Plugins;
using Stormancer.Diagnostics;

namespace Server.Plugins.Serials
{
   

    class SerialsController : ControllerBase
    {
      
        private readonly ILogger _logger;
        private readonly IUserSessions _session;

        public SerialsController(ILogger logger, IUserSessions session)
        {
            _logger = logger;
            _session = session;
        }


        /// <summary>
        /// Activate a key
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public  Task Activate(RequestContext<IScenePeerClient> ctx)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Get Keys activated by the connected user
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public  Task GetKeys(RequestContext<IScenePeerClient> ctx)
        {
            return Task.FromResult(true);
        }
      
    }


}
