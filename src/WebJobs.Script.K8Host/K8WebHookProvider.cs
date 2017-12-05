using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Script.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HttpHandler = Microsoft.Azure.WebJobs.IAsyncConverter<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage>;

namespace WebJobs.Script.K8Host
{
    internal class K8WebHookProvider : IWebHookProvider
    {
        private IDictionary<string, HttpHandler> _customHttpHandlers = new 
            ConcurrentDictionary<string, HttpHandler>(StringComparer.OrdinalIgnoreCase);

        public Uri GetUrl(IExtensionConfigProvider extension)
        {
            var extensionType = extension.GetType();
            var handler = extension as HttpHandler;
            if (handler == null)
            {
                throw new InvalidOperationException($"Extension must implement IAsyncConverter<HttpRequestMessage, HttpResponseMessage> in order to receive webhooks");
            }

            string name = extensionType.Name;
            _customHttpHandlers[name] = handler;

            var settings = ScriptSettingsManager.Instance;
            var hostName = settings.GetSetting("WEBSITE_HOSTNAME");
            if (hostName == null)
            {
                return null;
            }

            bool isLocalhost = hostName.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase);
            var scheme = isLocalhost ? "http" : "https";            
            return new Uri($"{scheme}://{hostName}/runtime/webhooks/{extensionType.Name}");
        }
    }
}
