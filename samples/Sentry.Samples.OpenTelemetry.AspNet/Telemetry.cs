using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Sentry.Samples.OpenTelemetry.AspNet
{
    public static class Telemetry
    {
        public const string ServiceName = "Sentry.Samples.OpenTelemetry.AspNet";
        public static ActivitySource ActivitySource { get; } = new ActivitySource(ServiceName);
    }
}