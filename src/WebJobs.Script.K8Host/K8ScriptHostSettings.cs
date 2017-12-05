using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebJobs.Script.K8Host
{
    public class K8ScriptHostSettings
    {
        public string ScriptPath { get; set; }

        public string LogPath { get; set; }

        public string SecretsPath { get; set; }

        public ILoggerFactoryBuilder LoggerFactoryBuilder { get; set; } = new K8LoggerFactoryBuilder();

        public K8ScriptHostSettings(IConfiguration configuration,
            ScriptSettingsManager scriptSettings)
        {
            this.ScriptPath  = configuration.GetValue<string>("ScriptPath");
            this.LogPath     = configuration.GetValue<string>("LogPath");
            this.SecretsPath = configuration.GetValue<string>("SecretsPath");            
        }
    }
}
