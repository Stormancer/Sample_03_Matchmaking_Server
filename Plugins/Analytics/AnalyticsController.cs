using Server.Plugins.API;
using Stormancer;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Analytics
{
    class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analytics;

        public AnalyticsController(IAnalyticsService analytics)
        {
            _analytics = analytics;
        }
        public Task Push(RequestContext<IScenePeerClient> ctx)
        {
            var docs = ctx.ReadObject<List<Document>>();
            foreach (var doc in docs)
            {
                _analytics.Push(doc.Type, doc.Content);
            }

            return Task.FromResult(true);

        }
    }
}
