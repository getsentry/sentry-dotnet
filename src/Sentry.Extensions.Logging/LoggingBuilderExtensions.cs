using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
// Ensures 'AddSentry' can be found without: 'using Sentry;'
namespace Microsoft.Extensions.Logging;

/// <summary>
/// LoggingBuilder extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder)
        => builder.AddSentry(null);

    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="optionsConfiguration">The options configuration.</param>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder, Action<SentryLoggingOptions>? optionsConfiguration)
    {
        builder.AddConfiguration();

        if (optionsConfiguration != null)
        {
            builder.Services.Configure(optionsConfiguration);
        }

        builder.Services.AddSingleton<IConfigureOptions<SentryLoggingOptions>, SentryLoggingOptionsSetup>();
        builder.Services.AddSingleton<ILoggerProvider, SentryLoggerProvider>();
        builder.Services.AddSingleton<ILoggerProvider, SentryStructuredLoggerProvider>();
        builder.Services.AddSentryHub();

        // All logs should flow to the SentryLogger, regardless of level.
        // Filtering of events is handled in SentryLogger, using SentryLoggingOptions.MinimumEventLevel
        // Filtering of breadcrumbs is handled in SentryLogger, using SentryLoggingOptions.MinimumBreadcrumbLevel
        builder.AddFilter<SentryLoggerProvider>(_ => true);

        // Logs from the SentryLogger should not flow to the SentryStructuredLogger as this may cause recursive invocations.
        // Filtering of structured logs is handled by Microsoft.Extensions.Configuration and Microsoft.Extensions.Options.
        builder.AddFilter<SentryStructuredLoggerProvider>("Sentry.ISentryClient", LogLevel.None);

        return builder;
    }
}
