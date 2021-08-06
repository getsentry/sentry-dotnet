using Sentry.Internals.DiagnosticSource;

namespace Sentry
{
    public static class SentryOptionsDiagnosticExtensions
    {
        /// <summary>
        /// Attaches Sentry to System Diagnostic Listeners events. 
        /// </summary>
        /// <param name="options">The Sentry options.</param>
        public static void AddDiagnosticListeners(this SentryOptions options)
            => options.AddIntegration(new SentryDiagnosticListenerIntegration());
    }
}
