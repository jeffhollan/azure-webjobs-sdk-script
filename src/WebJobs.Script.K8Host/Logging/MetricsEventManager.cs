using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebJobs.Script.K8Host.Logging
{
    public class MetricsEventManager : IDisposable
    {
        public MetricsEventManager(ScriptSettingsManager settingsManager, 
            IEventGenerator generator, 
            TimeSpan functionActivityFlushIntervalS,
            TimeSpan metricsFlushInterval)
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
