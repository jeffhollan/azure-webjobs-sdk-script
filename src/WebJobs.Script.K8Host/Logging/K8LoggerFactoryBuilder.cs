﻿using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebJobs.Script.K8Host.Logging
{
    public class K8LoggerFactoryBuilder : ILoggerFactoryBuilder
    {
        public void AddLoggerProviders(ILoggerFactory factory, 
            ScriptHostConfiguration scriptConfig, 
            ScriptSettingsManager settingsManager)
        {
            var x = "x";
        }
    }
}
