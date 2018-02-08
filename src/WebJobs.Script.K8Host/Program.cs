-using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
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
            try
            {
                RunAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
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
                    config.AddJsonFile("appsettings.json", optional: false, 
                        reloadOnChange: true);

                    var k8env = Environment.GetEnvironmentVariable("FUNCTIONS_K8CONFIG");
                    if (!String.IsNullOrEmpty(k8env) && File.Exists(k8env))
                        config.AddJsonFile(k8env, optional: false, 
                            reloadOnChange: true);
                    
                })
                .ConfigureLogging( (hostingContext, loggerFactory) =>
                {
                    var loggerConfig = GetLoggerConfiguration(hostingContext);
                    Log.Logger = loggerConfig.CreateLogger();
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

        internal static LoggerConfiguration GetLoggerConfiguration(
            WebHostBuilderContext context)
        {
            var loggerConfig = new LoggerConfiguration();

            // Add the relevant enrichment fields and properties; these environment properties must be 
            // set in the container orchestration definition 
            loggerConfig.Enrich.WithProperty("function.container", Environment.GetEnvironmentVariable("FUNCTION_POD_NAME"));
            loggerConfig.Enrich.WithProperty("function.node", Environment.GetEnvironmentVariable("FUNCTION_NODE_NAME"));
            loggerConfig.Enrich.WithProperty("function.namespace", Environment.GetEnvironmentVariable("FUNCTION_NAMESPACE_NAME"));
            loggerConfig.Enrich.WithProperty("function.container_ip", Environment.GetEnvironmentVariable("FUNCTION_POD_IP"));
            loggerConfig.Enrich.WithProperty("function.deployment", Environment.GetEnvironmentVariable("FUNCTION_DEPLOYMENT"));

            loggerConfig.Enrich.With<TimespanFormatter>();

            var serilogSection = context.Configuration.GetSection("Serilog");
            if (serilogSection != null && serilogSection.Value != null)
            {
                Console.WriteLine("Loading serilog configuration from master config");
                loggerConfig = loggerConfig.ReadFrom.Configuration(context.Configuration);
                return loggerConfig;
            }

            var serilogConfigFile = Environment.GetEnvironmentVariable("FUNCTIONS_LOGGING_CONFIG");
            if (!String.IsNullOrEmpty(serilogConfigFile) && File.Exists(serilogConfigFile))
            {
                Console.WriteLine("Loading serilog configuration from FUNCTIONS_LOGGING_CONFIG file {0}", serilogConfigFile);
                var config = new ConfigurationBuilder()
                    .AddJsonFile(serilogConfigFile)
                    .Build();
                loggerConfig = loggerConfig.ReadFrom.Configuration(config);
                return loggerConfig;
            }

            // Otherwise hard code the configuration for JSON output
            loggerConfig = loggerConfig.WriteTo.Console(new FluentDJsonFormatter(), 
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose);

            // loggerConfig.WriteTo.ApplicationInsights(iKey: "somemagickey");
            return loggerConfig;
        }
    }
     
}
