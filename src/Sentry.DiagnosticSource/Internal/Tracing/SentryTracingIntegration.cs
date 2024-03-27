using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal.Tracing;

internal class SentryTracingIntegration : ISdkIntegration
{
    /*
     TODO: Think about where to put this... would be good if it sat on an IDisposable but also something internal.
     We could possibly put it on the Hub - add an ITracingHub interface that was internal and held both this and
     the SentryTraceProvider. Alternatively we could rename IMetricHub to IInternalHub and put it there... metrics
     should probably be using this mechanism for tracing anyway (as it's all internal) but that would complicate
     things like code locations... which we could maybe store as custom properties on the Activity.
    */
    private SentryActivityListener? _listener;

    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.IsPerformanceMonitoringEnabled)
        {
            options.Log(SentryLevel.Info, "SentryTracing Integration is disabled because tracing is disabled.");
            return;
        }

        // TODO: Should we be registering this if OpenTelemetry is enabled?
        options.SentryTraceProvider = new SentryTraceProvider();
        _listener = new SentryActivityListener(hub);
    }
}
