using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server.Plugins.AdminApi
{
    interface IAdminWebApiConfig
    {
        void Configure(HttpConfiguration config);
    }
}
