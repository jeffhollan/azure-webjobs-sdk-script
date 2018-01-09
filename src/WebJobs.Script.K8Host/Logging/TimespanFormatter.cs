using Serilog.Core;
using Serilog.Events;
using System;

namespace WebJobs.Script.K8Host
{
    internal class TimespanFormatter : ILogEventEnricher
    {
        public const string TotalMs = "duration_ms";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.ContainsKey("duration"))
            {
                var sv = logEvent.Properties["duration"] as ScalarValue;
                switch(sv.Value)
                {
                    case TimeSpan ts:
                        var totalMs = ((TimeSpan)sv.Value).TotalMilliseconds;
                        logEvent.AddPropertyIfAbsent(new LogEventProperty(
                            TotalMs, new ScalarValue(totalMs)));
                        break;

                    case string str:
                        TimeSpan parsed;
                        if (TimeSpan.TryParse((string)sv.Value, out parsed))
                        {
                            logEvent.AddPropertyIfAbsent(new LogEventProperty(
                                TotalMs, new ScalarValue(parsed.TotalMilliseconds)));
                        }
                        break;

                    default:
                        break;
                }                 
            }
        }
    }
}