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
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Script.WebHost.Middleware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            // Add our script route handler
            services.AddSingleton<IWebJobsRouteHandler, ScriptRouteHandler>();
            services.AddHttpBindingRouting();            
            services.AddMvc();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, K8ScriptHostService>());

            //services.AddWebJobsScriptHostAuthentication();
            //services.AddWebJobsScriptHostAuthorization();

            var container = new DryIoc.Container();

            // Configuration for the script host and script settings
            ScriptSettingsManager.Instance.SetConfigurationFactory(() => Configuration);
            container.RegisterInstance(ScriptSettingsManager.Instance);

            var hostSettings = new K8ScriptHostSettings(Configuration, ScriptSettingsManager.Instance);
            container.RegisterInstance<K8ScriptHostSettings>(hostSettings);

            var scriptHostConfig = new ScriptHostConfiguration()
            {
                RootScriptPath = hostSettings.ScriptPath,
                RootLogPath = hostSettings.LogPath,
                FileLoggingMode = FileLoggingMode.Always,
                FileWatchingEnabled = true,
                IsSelfHost = true,
                HostHealthMonitorEnabled = true,                         
            };
            scriptHostConfig.HostConfig.LoggerFactory = _loggerFactory;
            scriptHostConfig.HostConfig.HostId = "todo-id";
            scriptHostConfig.HostConfig.Tracing.ConsoleLevel = System.Diagnostics.TraceLevel.Verbose;
            //scriptHostConfig.HostConfig.Tracing.Tracers.Add(new)

            container.RegisterInstance(scriptHostConfig);

            // Register the support services             
            container.Register<ILoggerFactoryBuilder, K8LoggerFactoryBuilder>();

            var metricsLogger = new K8MetricsLogger(
                _loggerFactory.CreateLogger("Functions.Metrics"));
                        
            container.RegisterInstance<IMetricsLogger>(metricsLogger);            
            container.RegisterInstance<IScriptEventManager>(new ScriptEventManager());
            container.RegisterInstance<ILoggerFactoryBuilder>(new K8LoggerFactoryBuilder());

            // Create the webjobs router
            //  container.RegisterInstance<IWebJobsRouter>(new WebJobsRouter())            
            container.RegisterDelegate<K8ScriptHostManager>((resolver) =>
            {
                var _scriptHostConfig = resolver.Resolve<ScriptHostConfiguration>();
                var _router = resolver.Resolve<IWebJobsRouter>();
                var _settingsManager = resolver.Resolve<ScriptSettingsManager>();

                return new K8ScriptHostManager(
                    config: _scriptHostConfig,
                    settingsManager: _settingsManager,
                    router: _router,
                    scriptHostFactory: null);
            }, reuse: Reuse.Singleton);

            // container.Register<K8ScriptHostManager>();

            //return services.AddWebJobsScriptHost(Configuration);           
            var provider = container
                .WithDependencyInjectionAdapter(services)
                .ConfigureServiceProvider<CompositionRoot>();
            
            var scriptHost = provider.GetRequiredService<K8ScriptHostManager>();

            return provider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder builder, 
            IApplicationLifetime applicationLifetime, IHostingEnvironment env, 
            ILoggerFactory loggerFactory)
        {            
            builder.UseMiddleware<FunctionInvocationMiddleware>();

            // Unless the call goes to teh /admin/host/status page, invoke the script
            // host check to ensure we are active and running
            builder.UseWhen(context => !context.Request.Path.StartsWithSegments("/admin/host/status"), config =>
            {
                config.UseMiddleware<ScriptHostCheckMiddleware>();
            });

            // Ensure the HTTP binding routing is registered after all middleware
            builder.UseHttpBindingRouting(applicationLifetime, null);

            builder.UseMvc(r =>
            {
                r.MapRoute(name: "Home",
                    template: string.Empty,
                    defaults: new { controller = "Home", action = "Get" });
            });
        }
    }

    internal class CompositionRoot
    {
    }
}
 