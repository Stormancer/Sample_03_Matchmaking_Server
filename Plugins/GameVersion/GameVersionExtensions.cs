using Server.Plugins.GameVersion;
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public static class GameVersionExtensions
    {
        public static void AddGameVersion(this ISceneHost builder, string prefix = null)
        {
            builder.Metadata[GameVersionPlugin.METADATA_KEY] = prefix ?? "default";
        }
    }
}
