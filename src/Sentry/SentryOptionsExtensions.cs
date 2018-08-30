using System;
using System.ComponentModel;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;

namespace Sentry
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryOptionsExtensions
    {
        /// <summary>
        /// Disables the strategy to detect duplicate events
        /// </summary>
        /// <remarks>
        /// In case a second event is being sent out from the same exception, that event will be discarded.
        /// It is possible the second event had in fact more data. In which case it'd be ideal to avoid the first
        /// event going out in the first place.
        /// </remarks>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableDuplicateEventDetection(this SentryOptions options)
            => options.Integrations = options.Integrations.RemoveAll(p => p.GetType() == typeof(DuplicateEventDetectionIntegration));

        /// <summary>
        /// Disables the capture of errors through <see cref="AppDomain.UnhandledException"/>
        /// </summary>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableAppDomainUnhandledExceptionCapture(this SentryOptions options)
            => options.Integrations = options.Integrations.RemoveAll(p => p.GetType() == typeof(AppDomainUnhandledExceptionIntegration));

        /// <summary>
        /// Add an integration
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="integration">The integration.</param>
        public static void AddIntegration(this SentryOptions options, ISdkIntegration integration)
            => options.Integrations = options.Integrations.Add(integration);

        /// <summary>
        /// Add prefix to exclude from 'InApp' stack trace list
        /// </summary>
        /// <param name="options"></param>
        /// <param name="prefix"></param>
        public static void AddInAppExclude(this SentryOptions options, string prefix)
            => options.InAppExclude = options.InAppExclude.Add(prefix);

        internal static void SetupLogging(this SentryOptions options)
        {
            if (options.Debug)
            {
                if (options.DiagnosticLogger == null)
                {
                    options.DiagnosticLogger = new ConsoleDiagnosticLogger(options.DiagnosticsLevel);
                    options.DiagnosticLogger?.LogDebug("Logging enabled with ConsoleDiagnosticLogger and min level: {0}", options.DiagnosticsLevel);
                }
            }
            else
            {
                options.DiagnosticLogger = null;
            }
        }
    }
}
