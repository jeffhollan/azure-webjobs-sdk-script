using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebJobs.Script.K8Host
{
    public static class HttpHostExtensions
    {
        public static IServiceProvider AddWebJobsScriptHost(this IServiceCollection services, 
            IConfiguration configuration)
        {
            // TODO - add script host routing
            // services.AddWebJobsScriptHostRouting();

            // TODO - handle this differently 
            //services.AddMvc()
                //.AddXmlDataContractSerializerFormatters();


            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, WebJobsScriptHostService>());

            // TODO: This is a direct port from the current model.
            // Some of those services (or the way we register them) may need to change
            var builder = new ContainerBuilder();

            // ScriptSettingsManager should be replaced. We're setting this here as a temporary step until
            // broader configuaration changes are made:
            ScriptSettingsManager.Instance.SetConfigurationFactory(() => configuration);
            builder.RegisterInstance(ScriptSettingsManager.Instance);

            builder.RegisterType<DefaultSecretManagerFactory>().As<ISecretManagerFactory>().SingleInstance();
            builder.RegisterType<ScriptEventManager>().As<IScriptEventManager>().SingleInstance();
            builder.RegisterType<DefaultLoggerFactoryBuilder>().As<ILoggerFactoryBuilder>().SingleInstance();
            builder.Register(c => WebHostSettings.CreateDefault(c.Resolve<ScriptSettingsManager>()));
            builder.RegisterType<WebHostResolver>().SingleInstance();

            // Temporary - This should be replaced with a simple type registration.
            builder.Register<IExtensionsManager>(c =>
            {
                var hostInstance = c.Resolve<WebScriptHostManager>().Instance;
                return new ExtensionsManager(hostInstance.ScriptConfig.RootScriptPath, hostInstance.TraceWriter, hostInstance.Logger);
            });

            // The services below need to be scoped to a pseudo-tenant (warm/specialized environment)
            builder.Register<WebScriptHostManager>(c => c.Resolve<WebHostResolver>().GetWebScriptHostManager()).ExternallyOwned();
            builder.Register<ISecretManager>(c => c.Resolve<WebHostResolver>().GetSecretManager()).ExternallyOwned();

            // Populate the container builder with registered services.
            // Doing this here will cause any services registered in the service collection to
            // override the registrations above
            builder.Populate(services);

            var applicationContainer = builder.Build();

            return new AutofacServiceProvider(applicationContainer);
        }
    }
}
