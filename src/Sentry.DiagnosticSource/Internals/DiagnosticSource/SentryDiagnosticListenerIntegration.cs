using System;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Internals.DiagnosticSource
{
    internal class SentryDiagnosticListenerIntegration : IInternalSdkIntegration
    {
        private SentryDiagnosticSubscriber? _subscriber;
        private IDisposable? _diagnosticListener;

        public void Register(IHub hub, SentryOptions options)
        {
            if (options.TracesSampleRate == 0)
            {
                options.Log(SentryLevel.Info, "DiagnosticSource Integration is now disabled due to TracesSampleRate being set to zero.");
                options.DisableDiagnosticSourceIntegration();
            }
            else
            {
                _subscriber = new SentryDiagnosticSubscriber(hub, options);
                _diagnosticListener = DiagnosticListener.AllListeners.Subscribe(_subscriber);
            }
        }

        public void Unregister(IHub hub)
        {
            _diagnosticListener?.Dispose();
            _subscriber?.Dispose();
        }
    }
}
