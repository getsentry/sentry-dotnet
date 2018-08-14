using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry;
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
        /// <returns></returns>
        public static ILoggingBuilder AddSentry(this ILoggingBuilder builder)
            => builder.AddSentry((Action<SentryLoggingOptions>)null);

        /// <summary>
        /// Adds the Sentry logging integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddSentry(this ILoggingBuilder builder, string dsn)
            => builder.AddSentry(o => o.Init(i =>
                {
                    if (!Dsn.IsDisabled(dsn))
                    {
                        // If it's invalid, let ctor throw
                        i.Dsn = new Dsn(dsn);
                    }
                }));

        /// <summary>
        /// Adds the Sentry logging integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="optionsConfiguration">The options configuration.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddSentry(
            this ILoggingBuilder builder,
            Action<SentryLoggingOptions> optionsConfiguration)
        {
            builder.AddConfiguration();

            if (optionsConfiguration != null)
            {
                builder.Services.Configure(optionsConfiguration);
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<SentryLoggingConfigurationOptions>, SentryLoggingConfigurationOptionsSetup>());
            builder.Services.AddSingleton<IConfigureOptions<SentryLoggingOptions>, SentryLoggingOptionsSetup>();

            builder.Services.AddSingleton<ILoggerProvider, SentryLoggerProvider>();

            builder.Services.AddSentry();
            return builder;
        }
    }
}
