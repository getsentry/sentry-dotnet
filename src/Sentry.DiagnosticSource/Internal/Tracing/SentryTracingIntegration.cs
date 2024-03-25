using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal.Tracing;

internal class SentryTracingIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.IsPerformanceMonitoringEnabled)
        {
            options.Log(SentryLevel.Info, "SentryTracing Integration is disabled because tracing is disabled.");
            return;
        }

        options.SentryTraceProvider = new SentryTraceProvider();
        // TODO: Also configure a listener : https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-collection-walkthroughs#collect-traces-using-custom-logic
    }
}
