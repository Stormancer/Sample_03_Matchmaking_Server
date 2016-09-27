using Server.Plugins.AdminApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server.Plugins.Serials
{
    class AdminWebApiConfig : IAdminWebApiConfig
    {
        public void Configure(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute("serials", "_serials/{action}", new { id = RouteParameter.Optional, Controller = "SerialsAdmin"});
        }
    }
}
