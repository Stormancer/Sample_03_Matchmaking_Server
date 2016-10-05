using System.Threading.Tasks;
using Stormancer.Plugins;
using Stormancer.Core;
using Stormancer.Configuration;
using System.Threading;

namespace Stormancer.Matchmaking
{
    public interface IMatchmakingService : IConfigurationRefresh
    {
      
        void Init(ISceneHost matchmakingScene);
        Task Run(CancellationToken ct);
        bool IsRunning { get; }
        Task FindMatch(RequestContext<IScenePeerClient> request);
        Task ResolveReadyRequest(Packet<IScenePeerClient> packet);
        Task CancelMatch(Packet<IScenePeerClient> packet);
        Task CancelMatch(IScenePeerClient peer);
    }
}