using MsgPack.Serialization;
using Newtonsoft.Json.Linq;
using Server.Database;
using Stormancer.Server.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Analytics
{
    public interface IAnalyticsService
    {
        void Push(string type, JObject document);

        void Push(string type, string document);

        Task Flush();
    }

    public struct Document
    {
        [MessagePackMember(0)]
        public string Type;
        [MessagePackMember(0)]
        public string Content;
    }

    class AnalyticsService : IAnalyticsService
    {

        private const string INDEX_TEMPLATE = "stats_{0}_{1}_{2}";

        private readonly ConcurrentQueue<Document> _documents = new ConcurrentQueue<Document>();
        private IESClientFactory _cFactory;
        private IEnvironment _environment;
        private static string _accountId;
        private static string _appName;
        private StringBuilder _buffer = new StringBuilder();

        public AnalyticsService(IESClientFactory clientFactory, IEnvironment environment)
        {
            _cFactory = clientFactory;
            _environment = environment;
        }
        public async Task Flush()
        {
            var index = await GetIndexName();



            var client = await _cFactory.CreateClient(index);

            Document doc;
            _buffer.Clear();
            var docCount = 0;
            while (_documents.TryDequeue(out doc))
            {
                ++docCount;
                _buffer.AppendLine($"{{\"index\":{{ \"_type\":\"{doc.Type}\"}}}}");
                _buffer.AppendLine(doc.Content);
            }
            if (docCount > 0)
            {
                var r = await client.LowLevel.BulkAsync<object>(index, _buffer.ToString());
            }


        }

        private async Task<string> GetIndexName()
        {
            if (_accountId == null || _appName == null)
            {
                var appInfos = await _environment.GetApplicationInfos();
                _accountId = appInfos.AccountId;
                _appName = appInfos.ApplicationName;
            }
            var week = DateTime.UtcNow.Ticks / (TimeSpan.TicksPerDay * 7);
            return string.Format(INDEX_TEMPLATE, _accountId, _appName, week);
        }
        public void Push(string type, string document)
        {

            _documents.Enqueue(new Document { Type = type, Content = document });
        }

        public void Push(string type, JObject document)
        {
            Push(type, document.ToString());
        }
    }
}
