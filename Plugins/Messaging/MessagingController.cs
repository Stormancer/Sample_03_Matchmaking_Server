using Server.Plugins.API;
using Server.Users;
using Stormancer.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins.Messaging
{
    class MessagingController : ControllerBase
    {
        private readonly IUserSessions _sessions;
        private ILogger _logger;
        public MessagingController(IUserSessions sessions, ILogger logger)
        {
            _sessions = sessions;
            _logger = logger;
        }
        public async Task Send(RequestContext<IScenePeerClient> ctx)
        {
          var targetId = ctx.ReadObject<string>();
            var target = await _sessions.GetPeer(targetId);
            var origin = await _sessions.GetUser(this.Request.RemotePeer);
            if (target == null)
            {
                throw new ClientException($"The user '{targetId}' is not connected");
            }
            var routes = target.Routes.Select(r => r.Name).ToArray();

            var memStream = new MemoryStream();
            ctx.InputStream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            target.Send("messaging.receive", s =>
            {
                target.Serializer().Serialize(origin.Id, s);
                memStream.CopyTo(s);
            }, Core.PacketPriority.MEDIUM_PRIORITY, Core.PacketReliability.RELIABLE);        }
    }
}
