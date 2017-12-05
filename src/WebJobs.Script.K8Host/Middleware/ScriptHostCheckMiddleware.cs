// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebJobs.Script.K8Host;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Middleware
{
    public class ScriptHostCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        private readonly K8ScriptHostManager  _scriptHostManager;
        private readonly IActionDescriptorCollectionProvider _actions;

        public ScriptHostCheckMiddleware(RequestDelegate next, 
            K8ScriptHostManager scriptHostManager, 
            ILoggerFactory loggerFactory,
            IActionDescriptorCollectionProvider actions)
        {
            _next = next;
            _scriptHostManager = scriptHostManager;
            _loggerFactory = loggerFactory;
            _actions = actions;
        }

        public async Task Invoke(HttpContext httpContext, K8ScriptHostManager manager)
        {
            //var routes = _actions.ActionDescriptors.Items.Select(x => new {
            //    Action = x.RouteValues["Action"],
            //    Controller = x.RouteValues["Controller"],
            //    Name = x.AttributeRouteInfo.Name,
            //    Template = x.AttributeRouteInfo.Template
            //}).ToList();
            //var response = JsonConvert.SerializeObject(routes);

            //// masimms todo - implement k8 compliant health and readiness checks
            //httpContext.Response.StatusCode = StatusCodes.Status200OK;
            //await httpContext.Response.WriteAsync(response);
            //return;

            // in standby mode, we don't want to wait for host start
            //bool bypassHostCheck = K8ScriptHostManager.InStandbyMode;

            //if (!bypassHostCheck)
            //{
            //    bool hostReady = await manager.DelayUntilHostReady();

            //    if (!hostReady)
            //    {
            //        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            //        await httpContext.Response.WriteAsync("Function host is not running.");

            //        return;
            //    }
            //}

            //if (StandbyManager.IsWarmUpRequest(httpContext.Request))
            //{
            //    await StandbyManager.WarmUp(httpContext.Request, _scriptHostManager);
            //}

            await _next.Invoke(httpContext);
        }
    }
}
