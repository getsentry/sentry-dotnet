using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Extensions.Logging.Internal;

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
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder)
        => builder.AddSentry((Action<SentryLoggingOptions>?)null);

    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">The DSN.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder, string dsn)
        => builder.AddSentry(o => o.Dsn = dsn);

    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="optionsConfiguration">The options configuration.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddSentry(this ILoggingBuilder builder, Action<SentryLoggingOptions>? optionsConfiguration)
    {
        builder.AddConfiguration();

        if (optionsConfiguration != null)
        {
            builder.Services.Configure(optionsConfiguration);
        }

        builder.AddSentryLoggingServices();
        return builder;
    }

    /// <summary>
    /// Adds the Sentry logging integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    internal static ISentryBuilder AddSentry(this ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.AddConfiguration();

        var section = configuration.GetSection("Sentry");
        builder.Services.Configure<SentryLoggingOptions>(section);

        builder.AddSentryLoggingServices();
        return new SentryBuilder(builder.Services);
    }

    private static void AddSentryLoggingServices(this ILoggingBuilder builder)
    {
        builder.Services.TryAddExactSingleton<IConfigureOptions<SentryLoggingOptions>, SentryLoggingOptionsSetup>();
        builder.Services.TryAddExactSingleton<ILoggerProvider, SentryLoggerProvider>();
        builder.Services.AddSentry<SentryLoggingOptions>();
    }
}
