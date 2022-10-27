using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal.DiagnosticSource;

internal class SentryDiagnosticListenerIntegration : ISdkIntegration
{
    private SentryDiagnosticSubscriber? _subscriber;
    private IDisposable? _diagnosticListener;

    public void Register(IHub hub, SentryOptions options)
    {
        if (options.TracesSampleRate == 0)
        {
            options.Log(SentryLevel.Info, "DiagnosticSource Integration is now disabled due to TracesSampleRate being set to zero.");
            options.DisableDiagnosticSourceIntegration();
            return;
        }

        _subscriber = new SentryDiagnosticSubscriber(hub, options);
        _diagnosticListener = DiagnosticListener.AllListeners.Subscribe(_subscriber);
    }
}
