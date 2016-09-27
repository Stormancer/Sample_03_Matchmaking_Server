using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;

namespace Server.Plugins.GameSession
{
    public interface IGameSessionService
    {
        void SetConfiguration(dynamic metadata);
        Task PostResults(Stream inputStream, IScenePeerClient remotePeer);
    }
}
