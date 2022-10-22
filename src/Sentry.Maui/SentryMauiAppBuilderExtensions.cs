using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
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

        builder.RegisterMauiEventsBinder();

        return builder;
    }

    private static void RegisterMauiEventsBinder(this MauiAppBuilder builder)
    {
        // Bind to MAUI events during the platform-specific application creating events.
        // Note that we used to do this in SentryMauiInitializer, but that approach caused the
        // IApplication instance to be constructed earlier than normal, having the side-effect
        // of interfering with a common approach to static DI in App.xaml.cs or AppShell.xaml.cs.
        // See https://github.com/getsentry/sentry-dotnet/issues/2001

        builder.ConfigureLifecycleEvents(events =>
        {
#if __IOS__
            events.AddiOS(lifecycle => lifecycle.WillFinishLaunching((application, _) =>
            {
                (application.Delegate as MauiUIApplicationDelegate)?.BindMauiEvents();
                return true;
            }));
#elif ANDROID
            events.AddAndroid(lifecycle => lifecycle.OnApplicationCreating(application =>
                (application as MauiApplication)?.BindMauiEvents()));
#elif WINDOWS
            events.AddWindows(lifecycle => lifecycle.OnLaunching((application, _) =>
                (application as MauiWinUIApplication)?.BindMauiEvents()));
#elif TIZEN
            events.AddTizen(lifecycle => lifecycle.OnCreate(application =>
                (application as MauiApplication)?.BindMauiEvents()));
#endif
        });
    }

    private static void BindMauiEvents(this IPlatformApplication application)
    {
        var binder = application.Services.GetRequiredService<MauiEventsBinder>();
        binder.BindMauiEvents();
    }
}
