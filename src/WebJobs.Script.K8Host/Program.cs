using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebJobs.Script.K8Host
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            RunAsync(args).GetAwaiter().GetResult();

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            string rootPath = Environment.CurrentDirectory;
            if (args.Length > 0)
            {                
                rootPath = (string)args[0];
            }
            Console.WriteLine("Using root path {0}", rootPath);

            var config = new ScriptHostConfiguration()
            {
                RootScriptPath = rootPath,
                IsSelfHost = true
            };

            // Override the logger factory to be cleaner for stdout output (container output)
            var scriptHostManager = new ScriptHostManager(config,
                loggerFactoryBuilder: new LoggerFactoryBuilder() );
            //scriptHostManager.RunAndBlock();


        }

        private static CancellationTokenSource _applicationCts 
            = new CancellationTokenSource();

        private static async Task RunAsync(string[] args)
        {
            var webhost = new WebHostBuilder()
                .UseKestrel()
                .UseLibuv()                
                .ConfigureAppConfiguration( (builderContext, config) =>
                {
                    var hostEnvironment = builderContext.HostingEnvironment;

                    // Add the registered configuration files
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                    var k8env = Environment.GetEnvironmentVariable("FUNCTIONS_K8CONFIG");
                    if (!String.IsNullOrEmpty(k8env) && File.Exists(k8env))
                        config.AddJsonFile(k8env);                                        
                })
                .ConfigureLogging( (hostingContext, loggerFactory) =>
                {
                    var logger = new LoggerConfiguration()
                        // TODO - pull from configuration
                        //.ReadFrom.Configuration(IConfiguration)
                        .WriteTo.Console()
                        .CreateLogger();
                    Log.Logger = logger;

                    loggerFactory.AddSerilog();
                })
                .UseDefaultServiceProvider( (context, options) =>
                {
                    
                })
                .UseStartup<Startup>()
                .Build();

            await webhost.RunAsync(_applicationCts.Token);            
        }

        internal static void InitiateShutdown()
        {
            _applicationCts.Cancel();
        }
    }

    public class LoggerFactoryBuilder : ILoggerFactoryBuilder
    {
        public void AddLoggerProviders(ILoggerFactory factory, 
            ScriptHostConfiguration scriptConfig, 
            ScriptSettingsManager settingsManager)
        {
            var logger = new LoggerConfiguration()
            //.ReadFrom.Configuration(IConfiguration)
                .WriteTo.Console()
                .CreateLogger();
            Log.Logger = logger;
            factory.AddSerilog();
        }
    }
}
