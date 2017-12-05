using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebJobs.Script.K8Host
{
    public class K8ScriptHostService : IHostedService
    {
        private readonly K8ScriptHostManager _scriptHostManager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private bool _disposed = false;
        private Task _hostTask;

        public K8ScriptHostService(K8ScriptHostManager scriptHostManager, 
            ILoggerFactory loggerFactory)
        {
            if (scriptHostManager == null)
                throw new ArgumentException($@"Unable to locate the {nameof(K8ScriptHostManager)} service. ");
            _scriptHostManager = scriptHostManager;

            _cancellationTokenSource = new CancellationTokenSource();
            _hostTask = Task.CompletedTask;
            _logger = loggerFactory.CreateLogger("Host.Service");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing WebScriptHostManager.");
            _hostTask = _scriptHostManager.RunAsync(_cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();

            var result = await Task.WhenAny(_hostTask, Task.Delay(TimeSpan.FromSeconds(10)));
            if (_hostTask.Status != TaskStatus.RanToCompletion)
            {
                _logger.LogWarning("Script host manager did not shutdown within its allotted time (10 sec).");
            }
            else
            {
                _logger.LogInformation("Script host manager shutdown completed.");
            }
        }
    }
}
