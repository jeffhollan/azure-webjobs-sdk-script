using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using DryIoc;
using Microsoft.Azure.WebJobs.Script.Eventing;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Azure.WebJobs.Script.Diagnostics;

namespace WebJobs.Script.K8Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }

        private ILoggerFactory _loggerFactory;

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            
            //services.AddWebJobsScriptHostAuthentication();
            //services.AddWebJobsScriptHostAuthorization();

            var container = new DryIoc.Container();

            ScriptSettingsManager.Instance.SetConfigurationFactory(() => Configuration);
            container.RegisterInstance(ScriptSettingsManager.Instance);

            // Register the support services             
            container.Register<ILoggerFactoryBuilder, K8LoggerFactoryBuilder>();

            var metricsLogger = new K8MetricsLogger(
                _loggerFactory.CreateLogger("Functions.Metrics"));
            container.RegisterInstance<IMetricsLogger>(metricsLogger);

            // TODO - k8 equivalent of secrets management
            //builder.RegisterType<DefaultSecretManagerFactory>().As<ISecretManagerFactory>().SingleInstance();
            
            // TODO - this looks fine as-is to port
            container.RegisterInstance(new ScriptEventManager());

            // Register the functions level logger
            container.RegisterInstance(new K8LoggerFactoryBuilder());

            //container.RegisterInstance()
            //container.RegisterDelegate<K8ScriptHostManager>((resolver) =>
            //{
                
            //});

            // Register the web 

            /*
             *  services.AddWebJobsScriptHostRouting();
            services.AddMvc()
                .AddXmlDataContractSerializerFormatters();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, WebJobsScriptHostService>());


            // ScriptSettingsManager should be replaced. We're setting this here as a temporary step until
            // broader configuaration changes are made:
            ScriptSettingsManager.Instance.SetConfigurationFactory(() => configuration);
            builder.RegisterInstance(ScriptSettingsManager.Instance);

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
*/

            //return services.AddWebJobsScriptHost(Configuration);
            return container.WithDependencyInjectionAdapter() as IServiceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
            IApplicationLifetime applicationLifetime, IHostingEnvironment env, 
            ILoggerFactory loggerFactory)
        {
            
            // TODO 
            //app.UseDeveloperExceptionPage();
            //app.UseWebJobsScriptHost(applicationLifetime);
        }
    }
}
 