using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Matchmaking
{


    public class MatchmakingConfig
    {
        public MatchmakingConfig(
            string matchmakingKind, Action<IDependencyBuilder> registerDependencies)
        {
            this._registerDependencies = registerDependencies;
            this.Kind = matchmakingKind;
        }
        private Action<IDependencyBuilder> _registerDependencies;
        public string Kind { get; set; }

        public void RegisterDependencies(IDependencyBuilder builder)
        {
            _registerDependencies?.Invoke(builder);
           
        }

    }


}
