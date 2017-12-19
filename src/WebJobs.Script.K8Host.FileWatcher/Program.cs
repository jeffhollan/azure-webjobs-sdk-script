using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using Serilog;
using Microsoft.Extensions.Logging;

namespace WebJobs.Script.K8Host.FileWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Write out the core startup information and context (including version and 
                // build times)
                var assemblyPath = typeof(Program).GetTypeInfo().Assembly;
                var filePath = (new Uri(assemblyPath.CodeBase)).LocalPath;
                var fileInfo = new FileInfo(filePath);
                Console.WriteLine($"[Loading] Executing k8 filewatcher built at {fileInfo.LastWriteTimeUtc} UTC version TODO");

                var configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true)                   
                   .AddEnvironmentVariables()
                   .AddCommandLine(args)
                   .Build();

                var serilogConfiguration = new LoggerConfiguration()
                    .WriteTo.Console();
                Log.Logger = serilogConfiguration.CreateLogger();
                var _loggerFactory = new LoggerFactory()                    
                    .AddSerilog();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error - " + ex.ToString());
            }
            
        }
    }
}
