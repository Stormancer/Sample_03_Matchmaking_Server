using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Owin;
using System.Web.Http.Dependencies;

namespace Server.Plugins.AdminApi
{
    class AdminApiPlugin : IHostPlugin
    {
        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostStarting += (Stormancer.Server.IHost host) =>
            {

                host.RegisterAdminApiFactory(builder =>
                {

                    var type = typeof(Microsoft.Owin.Builder.AppBuilder);
                    var config = new HttpConfiguration();
                    config.DependencyResolver = new DependencyResolver(host.DependencyResolver);
                    var configurators = host.DependencyResolver.ResolveAll<IAdminWebApiConfig>();
                    foreach(var c in configurators)
                    {
                        c.Configure(config);
                    }
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                   
                    builder.UseWebApi(config);
                });

            };
        }
    }

    public class DependencyResolver : System.Web.Http.Dependencies.IDependencyResolver
    {
        private readonly Stormancer.IDependencyResolver _resolver;
        private readonly DependencyScope _mainScope;
        public DependencyResolver(Stormancer.IDependencyResolver resolver)
        {
            _resolver = resolver;
            _mainScope = new DependencyScope(resolver);
        }

        public IDependencyScope BeginScope()
        {
            return new DependencyScope(_resolver.CreateChild());

        }

        public void Dispose()
        {

        }

        public object GetService(Type serviceType)
        {
            return _mainScope.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _mainScope.GetServices(serviceType);
        }

        private class DependencyScope : IDependencyScope
        {
            private readonly Stormancer.IDependencyResolver _resolver;
            public DependencyScope(Stormancer.IDependencyResolver resolver)
            {
                _resolver = resolver;
            }
            public void Dispose()
            {
                _resolver.Dispose();
            }

            public object GetService(Type serviceType)
            {
                return _resolver.GetType().GetMethod("Resolve").MakeGenericMethod(serviceType).Invoke(_resolver, new object[] { });
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {

                return (IEnumerable<object>)_resolver.GetType().GetMethod("ResolveAll").MakeGenericMethod(serviceType).Invoke(_resolver, new object[] { });


            }
        }
    }
}
