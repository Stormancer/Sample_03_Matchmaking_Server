using Server.Plugins.AdminApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server.Users
{
    class AdminWebApiConfig : IAdminWebApiConfig
    {
        public void Configure(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute("users.search", "_users/search", new { Controller = "UsersAdmin", Action="search"});
            config.Routes.MapHttpRoute("users", "_users/{id}", new { Controller = "UsersAdmin" });
        }
    }
}
