using System;
using System.ComponentModel;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace
// Ensures 'AddSentry' can be found without: 'using Sentry;'
namespace Microsoft.Extensions.Logging
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class LoggingBuilderExtensions
    {
        /// <summary>
        /// Adds the Sentry logging integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="optionsConfiguration">The options configuration.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddSentry(
            this ILoggingBuilder builder,
            Action<SentryLoggingOptions> optionsConfiguration = null)
        {
            var options = new SentryLoggingOptions();
            optionsConfiguration?.Invoke(options);

            builder.AddProvider(new SentryLoggerProvider(options));
            return builder;
        }
    }
}
