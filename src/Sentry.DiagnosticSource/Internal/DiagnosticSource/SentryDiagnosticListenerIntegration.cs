using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal.DiagnosticSource;

internal class SentryDiagnosticListenerIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.IsPerformanceMonitoringEnabled)
        {
            options.Log(SentryLevel.Info, "DiagnosticSource Integration is disabled because tracing is disabled.");
            return;
        }

        if (AotHelper.IsTrimmed)
        {
            options.Log(SentryLevel.Info, "DiagnosticSource Integration is disabled because trimming is enabled.");
            return;
        }

        var subscriber = new SentryDiagnosticSubscriber(hub, options);
        DiagnosticListener.AllListeners.Subscribe(subscriber);
    }
}
