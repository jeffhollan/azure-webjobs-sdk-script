using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebJobs.Script.K8Host
{
    public class K8MetricsLogger : IMetricsLogger
    {
        private ILogger _logger;

        public K8MetricsLogger(ILogger logger)
        {
            _logger = logger;
        }

        public object BeginEvent(string eventName, string functionName = null)
        {
            var evt = new K8MetricEvent()
            {
                FunctionName = functionName,
                EventName = eventName.ToLowerInvariant(),
                Timestamp = DateTime.UtcNow
            };

            return evt;
        }

        public void BeginEvent(MetricEvent metricEvent)
        {
            var x = "x";
        }

        public void EndEvent(MetricEvent metricEvent)
        {
            var completedEvent = metricEvent as FunctionStartedEvent;
            if (completedEvent != null)
            {
                _logger.LogInformation("");
            }
            else
            {

            }
            var x = "x";
        }

        public void EndEvent(object eventHandle)
        {
            var x = "x";
        }

        public void LogEvent(MetricEvent metricEvent)
        {
            var x = "x";
        }

        public void LogEvent(string eventName, string functionName = null)
        {
            var x = "x";
        }
    }
}
