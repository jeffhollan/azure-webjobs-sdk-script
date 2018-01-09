using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Loggers;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebJobs.Script.K8Host
{
    public class K8ScriptHostManager : ScriptHostManager
    {
        private readonly ScriptHostConfiguration _config;
        private readonly IWebJobsRouter _router;
        private readonly IMetricsLogger _metricsLogger;
        private readonly IWebHookProvider _bindingWebHookProvider;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly ILoggerFactory _loggerFactory;

        private Task _runTask;
        private int _isRunning = 0;

        public K8ScriptHostManager(
            ScriptHostConfiguration config,
            ScriptSettingsManager settingsManager,
            IWebJobsRouter router,
            IScriptHostFactory scriptHostFactory = null, 
            ILoggerFactoryBuilder loggerFactoryBuilder = null)
                : base(config, settingsManager, scriptHostFactory, 
                      loggerFactoryBuilder: loggerFactoryBuilder)
        {
            this._config = config;
            this._router = router;

            // Create a metrics logger
            _loggerFactory = _config.HostConfig.LoggerFactory;
            _metricsLogger = new K8MetricsLogger(_loggerFactory.CreateLogger("Metrics.ScriptHost"));

            _bindingWebHookProvider = new K8WebHookProvider();
            _exceptionHandler = new K8ScriptHostExceptionHandler(this);
        }
        
        public Task RunAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 0)
            {
                _runTask = Task.Factory.StartNew(
                    () => RunAndBlock(cancellationToken),
                    TaskCreationOptions.LongRunning);
            }            
            return _runTask;
        }

        public override ScriptHost Instance => base.Instance;

        public override ScriptHostState State => base.State;

        public override Exception LastError => base.LastError;

     
      
        public override void RestartHost()
        {
            base.RestartHost();
        }

        public override void Shutdown()
        {
            string message = "Environment shutdown has been triggered. Stopping host and signaling shutdown.";
            Instance?.TraceWriter.Info(message);
            Instance?.Logger?.LogInformation(message);

            Stop();

            Program.InitiateShutdown();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnCreatingHost()
        {
            base.OnCreatingHost();
        }

        protected override void OnHostInitialized()
        {
            // Initialize HTTP
            var extensions = Instance.ScriptConfig.HostConfig.GetService<IExtensionRegistry>();
            var httpConfig = extensions
                .GetExtensions<IExtensionConfigProvider>()
                .OfType<HttpExtensionConfiguration>().Single();
            InitializeHttpFunctions(Instance.Functions, httpConfig);

            base.OnHostInitialized();
        }

        protected override void OnInitializeConfig(ScriptHostConfiguration config)
        {
            base.OnInitializeConfig(config);

            // Note: this method can be called many times for the same ScriptHostConfiguration
            // so no changes should be made to the configuration itself. It is safe to modify
            // ScriptHostConfiguration.Host config though, since the inner JobHostConfiguration
            // is created on each restart.

            // Add the host specific services
            config.HostConfig.AddService<IMetricsLogger>(_metricsLogger);
            config.HostConfig.AddService<IWebHookProvider>(_bindingWebHookProvider);
            config.HostConfig.AddService<IWebJobsExceptionHandler>(_exceptionHandler);

            // Set the host ID (in a K8 scenario, this will be the pod id)
            var hostId = config.HostConfig.HostId ?? "default";
            var functionLogger = _loggerFactory.CreateLogger("Function.Logger");

            var instanceLogger = new K8FunctionInstanceLogger(
                (name) => this.Instance.GetFunctionOrNull(name),
                (IMetricsLogger)_metricsLogger, (ILogger)functionLogger, hostId);
            config.HostConfig.AddService<IAsyncCollector<FunctionInstanceLogEntry>>(instanceLogger);

            // Disable standard dashboard logging; enable custom container logging
            config.HostConfig.DashboardConnectionString = null;

        }

        protected override void OnHostStarted()
        {         
            // Activate the readiness check
            // masimms TODO 

            base.OnHostStarted();
        }

        private void InitializeHttpFunctions(
            IEnumerable<FunctionDescriptor> functions,
            HttpExtensionConfiguration httpConfig)
        {
            // Clear prior routes
            _router.ClearRoutes();            

            // Create the router instances
            var scriptRouteHandler = new ScriptRouteHandler(_config.HostConfig.LoggerFactory, () => Instance);
            var routeBuilder = _router.CreateBuilder(scriptRouteHandler, httpConfig.RoutePrefix);

            // Note that proxies do not honor the host.json route prefix
            var proxyRouteHandler = new ScriptRouteHandler(_config.HostConfig.LoggerFactory, () => Instance);
            var proxyRouteBuilder = _router.CreateBuilder(proxyRouteHandler, routePrefix: null);

            foreach (var function in functions)
            {
                var httpTrigger = function.GetTriggerAttributeOrNull<HttpTriggerAttribute>();
                if (httpTrigger != null)
                {
                    var constraints = new RouteValueDictionary();
                    if (httpTrigger.Methods != null)
                    {
                        constraints.Add("httpMethod", new HttpMethodRouteConstraint(httpTrigger.Methods));
                    }

                    string route = httpTrigger.Route;

                    if (string.IsNullOrEmpty(route))
                    {
                        route = function.Name;
                    }
                    
                    if (function.Metadata.IsProxy)
                        proxyRouteBuilder.MapFunctionRoute(function.Metadata.Name, route, constraints, function.Metadata.Name);
                    else
                        routeBuilder.MapFunctionRoute(function.Metadata.Name, route, constraints, function.Metadata.Name);                    
                }
            }

            // Proxy routes will take precedence over http trigger functions
            // so they will be added first to the router.
            if (proxyRouteBuilder.Count > 0)
            {
                _router.AddFunctionRoute(proxyRouteBuilder.Build());
            }

            if (routeBuilder.Count > 0)
            {
                _router.AddFunctionRoute(routeBuilder.Build());
            }
            
        }
    }
}
