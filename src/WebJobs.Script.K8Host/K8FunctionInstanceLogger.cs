using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Loggers;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebJobs.Script.K8Host
{
    internal class K8FunctionInstanceLogger : IAsyncCollector<FunctionInstanceLogEntry>
    {
        private const string Key = "metadata";

        private readonly Func<string, FunctionDescriptor> _funcLookup;
        private readonly IMetricsLogger _metrics;
        private readonly ILogger _logger;

        public K8FunctionInstanceLogger(
          Func<string, FunctionDescriptor> funcLookup,
          IMetricsLogger metrics,
          ILogger logger,
          string hostName)
        {
            _funcLookup = funcLookup;
            _metrics = metrics;
            _logger = logger;
        }

        public Task AddAsync(FunctionInstanceLogEntry item, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            FunctionInstanceMonitor state;
            item.Properties.TryGetValue(Key, out state);

            if (item.EndTime.HasValue)
            {
                // Completed
                bool success = item.ErrorDetails == null;
                state.End(success);
            }
            else
            {
                // Started
                if (state == null)
                {
                    string shortName = Utility.GetFunctionShortName(item.FunctionName);

                    FunctionDescriptor descr = _funcLookup(shortName);
                    if (descr == null)
                    {
                        // This exception will cause the function to not get executed.
                        throw new InvalidOperationException($"Missing function.json for '{shortName}'.");
                    }
                    state = new FunctionInstanceMonitor(descr.Metadata, _metrics, item.FunctionInstanceId, descr.Invoker.FunctionLogger);
                    item.Properties[Key] = state;
                    state.Start();
                }
            }

            _logger.LogInformation("{functionInstanceId} {functionName} {startTime} {endTime} {triggerReason} {arguments} {errorDetails} {logOutput} {parentId}",
                item.FunctionInstanceId, Utility.GetFunctionShortName(item.FunctionName),
                item.StartTime, item.EndTime, item.TriggerReason,
                item.Arguments, item.ErrorDetails, item.LogOutput,
                item.ParentId);

            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
