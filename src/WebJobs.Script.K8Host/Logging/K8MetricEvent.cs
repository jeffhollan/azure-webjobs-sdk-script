using Microsoft.Azure.WebJobs.Script.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebJobs.Script.K8Host
{
    public class K8MetricEvent : MetricEvent
    {
        public string EventName { get; set;  }
        public double Value { get; set; }
    }
}
