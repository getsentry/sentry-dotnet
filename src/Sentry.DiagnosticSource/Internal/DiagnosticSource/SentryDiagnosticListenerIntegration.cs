using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal.DiagnosticSource;

internal class SentryDiagnosticListenerIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.IsTracingEnabled)
        {
            options.Log(SentryLevel.Info, "DiagnosticSource Integration is disabled because tracing is disabled.");
            options.DisableDiagnosticSourceIntegration();
            return;
        }

        var subscriber = new SentryDiagnosticSubscriber(hub, options);
        DiagnosticListener.AllListeners.Subscribe(subscriber);
    }
}
