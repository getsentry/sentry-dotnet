using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Maui;
using Sentry.Maui.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Maui.Hosting;

/// <summary>
/// Sentry extensions for <see cref="MauiAppBuilder"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryMauiAppBuilderExtensions
{
    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static MauiAppBuilder UseSentry(this MauiAppBuilder builder)
        => UseSentry(builder, (Action<SentryMauiOptions>?)null);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">The DSN.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static MauiAppBuilder UseSentry(this MauiAppBuilder builder, string dsn)
        => builder.UseSentry(o => o.Dsn = dsn);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static MauiAppBuilder UseSentry(this MauiAppBuilder builder,
        Action<SentryMauiOptions>? configureOptions)
    {
        var services = builder.Services;
        services.Configure<SentryMauiOptions>(options =>
            builder.Configuration.GetSection("Sentry").Bind(options));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddLogging();
        services.AddSingleton<ILoggerProvider, SentryLoggerProvider>();
        services.AddSingleton<IMauiInitializeService, SentryMauiInitializer>();
        services.AddSingleton<IConfigureOptions<SentryMauiOptions>, SentryMauiOptionsSetup>();
        services.AddSingleton<Disposer>();
        services.AddSingleton<MauiEventsBinder>();

        services.AddSentry<SentryMauiOptions>();

        return builder;
    }
}
