using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics;
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
            switch (metricEvent)
            {
                case FunctionStartedEvent fse:
                    fse.Timestamp = DateTime.UtcNow;
                    break;

                default:
                    break;
            }            
        }

        public void EndEvent(MetricEvent metricEvent)
        {            
            switch(metricEvent)
            {
                case FunctionStartedEvent fse:
                    fse.Duration = DateTime.UtcNow - fse.Timestamp;
                    _logger.LogInformation("{eventType} {functionName} {timestamp} {duration} {invocationId} {success}",
                        "Function", fse.FunctionName, fse.Timestamp, fse.Duration, fse.InvocationId, fse.Success);                    
                    break;

                case SystemMetricEvent sme:
                    _logger.LogInformation("{eventType} {functionName} {timestamp} {duration} {minimum} {maximum} {average} {count}",
                        sme.EventName, sme.FunctionName, sme.Timestamp, sme.Duration, 
                        sme.Minimum, sme.Maximum, sme.Average, sme.Count);
                    break;

                case K8MetricEvent kme:
                    if (kme.Duration == TimeSpan.Zero)
                        kme.Duration = DateTime.UtcNow.Subtract(kme.Timestamp);
                    _logger.LogInformation("{eventType} {functionName} {timestamp} {duration} {value}",
                      kme.EventName, kme.FunctionName, kme.Timestamp, kme.Duration, kme.Value);                    
                    break;

                default:
                    _logger.LogInformation("{eventType} {functionName} {timestamp} {duration}",
                        "Generic", metricEvent.FunctionName, metricEvent.Timestamp, metricEvent.Duration);                    
                    break;
            }            
        }

        public void EndEvent(object eventHandle)
        {
            if (eventHandle == null) return;

            switch(eventHandle)
            {
                case MetricEvent me:
                    EndEvent(me);
                    break;

                default:
                    _logger.LogInformation("{eventType} Unformatted end event {type} {string}",
                        "UnknownEventType", eventHandle.GetType().AssemblyQualifiedName, eventHandle.ToString());
                    break;
            }            
        }

        public void LogEvent(MetricEvent metricEvent)
        {
            switch(metricEvent)
            {
                case HostStarted hse:
                    _logger.LogInformation("{eventType} {functionName} {timestamp} {duration} {hostId}",
                       "Host", "HostStarted", metricEvent.Timestamp, metricEvent.Duration, hse.Host.InstanceId);
                    break;

                default:
                    _logger.LogInformation("{eventType} {functionName} {timestamp} {duration}",
                       "Generic", metricEvent.FunctionName, metricEvent.Timestamp, metricEvent.Duration);
                    break;
            }            
        }

        public void LogEvent(string eventName, string functionName = null)
        {
            _logger.LogInformation("{eventType} {functionName} {timestamp}",
                eventName, functionName, DateTime.UtcNow);            
        }
    }
}
