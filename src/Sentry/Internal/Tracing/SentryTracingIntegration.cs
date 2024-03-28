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

        // TODO: Should we be registering this if OpenTelemetry is enabled?
        options.InternalTraceProvider = new SentryTraceProvider(hub);
    }
}
