using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server.Plugins.Serials
{
    public class SerialsAdminController : ApiController
    {
        private readonly ISerialsService _serials;
        public SerialsAdminController(ISerialsService serials)
        {
            _serials = serials;

        }

        [HttpGet]
        [ActionName("search")]
        public async Task<IEnumerable<SerialKeySummary>> Search(string query = "", int skip = 0, int take = 10)
        {

            var keys = await _serials.QueryKeys(query, skip, take);

            return keys.Select(s => new SerialKeySummary { Key = s.Id, Status = s.Status.ToString() });
        }

        public Task<SerialKey> Get(string id)
        {
            return _serials.Get(id);
        }

        [HttpPost]
        public Task ActivateKey(string id, string ownerId, string comment)
        {
            return _serials.ActivateKey(id, ownerId, GetAdminUserId(), comment);
        }

        [HttpPost]
        public Task ChangeKeyStatus(string id, KeyStatus keyStatus, string comment)
        {
            return _serials.ChangeKeyStatus(id, keyStatus, GetAdminUserId(), comment);
        }
        private string GetAdminUserId()
        {
            return Request.Headers.GetValues("x-user").FirstOrDefault();
        }


        [HttpPost]
        public async Task<IEnumerable<string>> CreateKeys([FromBody]CreateKeysViewModel vm)
        {
            var serials = new List<string>();
            for (int i = 0; i < vm.Count; i++)
            {

                serials.Add(await _serials.CreateKey(vm.Content, GetAdminUserId(), vm.Comment));
            }

            return serials;
        }
    }

    public class CreateKeysViewModel
    {
        public int Count { get; set; }

        public JObject Content { get; set; }

        public string Comment { get; set; }
    }

}
