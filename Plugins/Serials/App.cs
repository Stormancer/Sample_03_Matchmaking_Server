using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Serials
{
    public class App
    {
        public void Run(IAppBuilder builder)
        {
            builder.AdminPlugin("serials").Name("Serial key");
            builder.AddPlugin(new SerialsPlugin());
        }
    }
}
