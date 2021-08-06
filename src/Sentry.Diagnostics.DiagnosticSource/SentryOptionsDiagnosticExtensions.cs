using Sentry.Internals.DiagnosticSource;

namespace Sentry
{
    /// <summary>
    /// The additional Sentry Options extensions from Sentry Diagnostic Listener.
    /// </summary>
    public static class SentryOptionsDiagnosticExtensions
    {
        /// <summary>
        /// Attach Sentry to System Diagnostic Listeners events.
        /// </summary>
        /// <param name="options">The Sentry options.</param>
        public static void AddDiagnosticListeners(this SentryOptions options)
            => options.AddIntegration(new SentryDiagnosticListenerIntegration());
    }
}
