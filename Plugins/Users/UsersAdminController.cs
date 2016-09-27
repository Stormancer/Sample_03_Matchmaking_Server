using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server.Users
{
    public class UsersAdminController : ApiController
    {
        private readonly IUserService _users;

        public UsersAdminController(IUserService users)
        {
            _users = users;
        }

        [HttpGet]
        [ActionName("search")]
        public async Task<IEnumerable<UserViewModel>> Search(string query, int take = 20, int skip = 0)
        {
            var users = await _users.Query(query, take, skip);

            var results = new List<UserViewModel>();

            foreach (var user in users)
            {
                var usr = new UserViewModel { id = user.Id, channels = user.Channels, userData = user.UserData };
                dynamic auth = user.Auth[LoginPasswordAuthenticationProvider.PROVIDER_NAME];
                usr.channels["email"] = JObject.FromObject(new { value = auth.email });
                results.Add(usr);

            }
            return results;
        }

        [HttpDelete]
        public Task Delete(string id)
        {
            return _users.Delete(id);
    }
    }
   

    public class UserViewModel
    {
        public string id { get; set; }
        public JObject userData { get; set; }
        public JObject channels { get; set; }

    }
}
