using System;
using System.Diagnostics;
using Sentry.Internal;

namespace Sentry.Internals.DiagnosticSource
{
    internal class SentryDiagnosticListenerIntegration : IInternalSdkIntegration
    {
        private IDisposable? _diagnosticListener { get; set; }
        public void Register(IHub hub, SentryOptions options)
        {
            _diagnosticListener = DiagnosticListener.AllListeners.Subscribe(new SentryDiagnosticSubscriber(hub, options));
        }

        public void Unregister(IHub hub)
        {
            _diagnosticListener?.Dispose();
        }
    }
}
