using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;

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
    /// <remarks>
    /// Apps that call <c>UseSentry</c> don't need this, because <c>UseSentry</c> already adds the logging integration.
    /// If your app calls <c>ClearProviders()</c> after calling <c>UseSentry</c> that would also remove Sentry's logging
    /// integration - in which case this method can be used to add Sentry's providers back again.
    /// </remarks>
    /// <param name="builder">The builder.</param>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder)
        => builder.AddSentry((Action<SentryLoggingOptions>?)null);

    /// <summary>
    /// Not supported. The logging integration no longer initializes the SDK.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">No longer supported.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    [Obsolete(SentryLoggingOptions.ObsoleteSdkInitialization, error: true)]
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder, string dsn)
        => throw new NotSupportedException(SentryLoggingOptions.ObsoleteSdkInitialization);

    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <remarks>
    /// Apps that call <c>UseSentry</c> don't need this, because <c>UseSentry</c> already adds the logging integration.
    /// If your app calls <c>ClearProviders()</c> after calling <c>UseSentry</c> that would also remove Sentry's logging
    /// integration - in which case this method can be used to add Sentry's providers back again. The providers it adds
    /// back use the host's Sentry options, so <paramref name="optionsConfiguration"/> is ignored.
    /// </remarks>
    /// <param name="builder">The builder.</param>
    /// <param name="optionsConfiguration">The options configuration.</param>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder, Action<SentryLoggingOptions>? optionsConfiguration)
    {
        builder.AddConfiguration();

        if (optionsConfiguration != null)
        {
            builder.Services.Configure(optionsConfiguration);
        }

        var services = builder.Services;
        builder.Services.AddSingleton<IConfigureOptions<SentryLoggingOptions>, SentryLoggingOptionsSetup>();
        builder.Services.AddSingleton<ILoggerProvider>(c =>
        {
            if (!HostLoggerProviders.WereAdded(c))
            {
                return new SentryLoggerProvider(c.GetRequiredService<IOptions<SentryLoggingOptions>>(), HubAdapter.Instance);
            }

            if (HostLoggerProviders.AreRegistered(c, services))
            {
                c.GetService<IHub>()?.GetSentryOptions()?.LogWarning(SentryLoggingOptions.RedundantWithHostIntegration);
                return NullLoggerProvider.Instance;
            }

            if (optionsConfiguration != null)
            {
                c.GetService<IHub>()?.GetSentryOptions()?.LogWarning(SentryLoggingOptions.OptionsIgnoredWithHostIntegration);
            }

            return HostLoggerProviders.CreateEvents(c);
        });
        builder.Services.AddSingleton<ILoggerProvider>(c =>
            !HostLoggerProviders.WereAdded(c) ? new SentryStructuredLoggerProvider(HubAdapter.Instance)
            : HostLoggerProviders.AreRegistered(c, services) ? NullLoggerProvider.Instance
            : HostLoggerProviders.CreateStructuredLogs(c));
        builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(new SentryHttpMessageHandlerBuilderFilter(() => HubAdapter.Instance));

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
