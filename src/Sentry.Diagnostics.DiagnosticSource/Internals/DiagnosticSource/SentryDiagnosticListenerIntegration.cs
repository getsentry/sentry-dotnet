using System.Diagnostics;
using Sentry.Integrations;

namespace Sentry.Internals.DiagnosticSource
{
    internal class SentryDiagnosticListenerIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            DiagnosticListener.AllListeners.Subscribe(new SentryDiagnosticSubscriber(hub, options));
        }
    }
}
