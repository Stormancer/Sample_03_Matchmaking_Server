using Server.Plugins.Serials;
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public static class SerialsExtensions
    {
        public static void AddSerials(this ISceneHost scene)
        {
            scene.Metadata[SerialsPlugin.METADATA_KEY] = "enabled";
        }
    }
}
