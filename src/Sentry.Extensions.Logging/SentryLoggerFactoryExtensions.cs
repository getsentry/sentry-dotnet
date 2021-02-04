using System;
using System.ComponentModel;
using Sentry;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;
using Sentry.Internal;

// ReSharper disable once CheckNamespace
// Ensures 'AddSentry' can be found without: 'using Sentry;'
namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// SentryLoggerFactory extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds the Sentry logging integration.
        /// </summary>
        /// <remarks>
        /// This method does not need to be called when calling `UseSentry` with ASP.NET Core
        /// since that integrates with the logging framework automatically.
        /// </remarks>
        /// <param name="factory">The factory.</param>
        /// <param name="optionsConfiguration">The options configuration.</param>
        /// <returns></returns>
        public static ILoggerFactory AddSentry(
            this ILoggerFactory factory,
            Action<SentryLoggingOptions>? optionsConfiguration = null)
        {
            var options = new SentryLoggingOptions();

            optionsConfiguration?.Invoke(options);

            if (options.DiagnosticLogger == null)
            {
                var logger = factory.CreateLogger<ISentryClient>();
                options.DiagnosticLogger = new MelDiagnosticLogger(logger, options.DiagnosticLevel);
            }

            IHub hub;
            if (options.InitializeSdk)
            {
                if (SentrySdk.IsEnabled && options.Dsn is null)
                {
                    options.DiagnosticLogger?.LogWarning("Not calling Init from {0} because SDK is already enabled and no DSN was provided to the integration", nameof(SentryLoggerFactoryExtensions));
                    hub = HubAdapter.Instance;
                }
                else
                {
                    options.DiagnosticLogger?.LogDebug("Initializing from {0} and swapping current Hub.", nameof(SentryLoggerFactoryExtensions));
                    hub = SentrySdk.InitHub(options);
                    SentrySdk.UseHub(hub);
                }
            }
            else
            {
                // Access to whatever the SentrySdk points to (disabled or initialized via SentrySdk.Init)
                hub = HubAdapter.Instance;
            }

            factory.AddProvider(new SentryLoggerProvider(hub, SystemClock.Clock, options));
            return factory;
        }
    }
}
