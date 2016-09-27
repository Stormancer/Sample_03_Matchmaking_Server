using Server.Plugins.API;
using Stormancer;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.GameSession
{
    class GameSessionController:ControllerBase
    {
        private IGameSessionService _service;

        public GameSessionController(IGameSessionService service)
        {
            _service = service;
        }
        public Task PostResults(RequestContext<IScenePeerClient> ctx)
        {
            return _service.PostResults(ctx.InputStream, ctx.RemotePeer);
        }
        
    }
}
