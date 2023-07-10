using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.AspNet;
using Sentry.Extensibility;
using Sentry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Samples.OpenTelemetry.AspNet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private IDisposable _sentry;
        private TracerProvider _tracerProvider;

        protected void Application_Start()
        {
            var builder = Sdk.CreateTracerProviderBuilder()
                .AddAspNetInstrumentation()

                // Other configuration, like adding an exporter and setting resources
                .AddSource(Telemetry.ServiceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: Telemetry.ServiceName, serviceVersion: "1.0.0"));

            // Initialize Sentry to capture AppDomain unhandled exceptions and more.
            _sentry = SentrySdk.Init(o =>
            {
                //o.Dsn = "...Your DSN...";
                o.Dsn = "https://665adc0bb0024947a0aa92522188a128@o1197552.ingest.sentry.io/4505502182604800";
                o.Debug = true;
                o.TracesSampleRate = 1.0;
                o.AddAspNet(RequestSize.Always);
                o.UseOpenTelemetry(builder);
            });

            _tracerProvider = builder.Build();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Global error catcher
        protected void Application_Error() => Server.CaptureLastError();

        protected void Application_BeginRequest()
        {
            Context.StartSentryTransaction();
        }

        protected void Application_EndRequest()
        {
            Context.FinishSentryTransaction();
        }

        protected void Application_End()
        {
            _tracerProvider?.Dispose();
            _sentry?.Dispose();
        }
    }
}
