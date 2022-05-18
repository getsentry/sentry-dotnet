using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Maui;

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
        // TODO: Verify that each of these dependencies is needed.

        // builder.Logging.AddConfiguration();

        builder.Services.Configure<SentryMauiOptions>(options =>
            builder.Configuration.GetSection("Sentry").Bind(options));

        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        builder.Services.AddSingleton<IMauiInitializeService, SentryMauiInitializer>();
        builder.Services.AddSingleton<IConfigureOptions<SentryMauiOptions>, SentryMauiOptionsSetup>();
        builder.Services.AddSentry<SentryMauiOptions>();

        return builder;
    }
}
