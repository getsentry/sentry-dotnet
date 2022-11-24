using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal.DiagnosticSource;

internal class SentryDiagnosticListenerIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        if (options.TracesSampleRate == 0 && options.TracesSampler == null)
        {
            options.Log(SentryLevel.Info, "DiagnosticSource Integration is now disabled due to TracesSampleRate being set to zero, and no TracesSampler set.");
            options.DisableDiagnosticSourceIntegration();
            return;
        }

        var subscriber = new SentryDiagnosticSubscriber(hub, options);
        DiagnosticListener.AllListeners.Subscribe(subscriber);
    }
}
