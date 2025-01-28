using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
using Sentry.Extensibility;
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

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddLogging();
        services.AddSingleton<ILoggerProvider, SentryMauiLoggerProvider>();
        services.AddSingleton<IMauiInitializeService, SentryMauiInitializer>();
        services.AddSingleton<IConfigureOptions<SentryMauiOptions>, SentryMauiOptionsSetup>();
        services.AddSingleton<Disposer>();
        services.TryAddSingleton<IMauiEventsBinder, MauiEventsBinder>();

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
            events.AddiOS(lifecycle =>
            {
                lifecycle.FinishedLaunching((application, launchOptions) =>
                {
                    // A bit of hackery here, because we can't mock UIKit.UIApplication in tests.
                    var platformApplication = application != null!
                        ? application.Delegate as IPlatformApplication
                        : launchOptions["application"] as IPlatformApplication;

                    platformApplication?.HandleMauiEvents();
                    return true;
                });
                lifecycle.WillTerminate(application =>
                {
                    if (application == null!)
                    {
                        return;
                    }

                    var platformApplication = application.Delegate as IPlatformApplication;
                    platformApplication?.HandleMauiEvents(bind: false);

                    //According to https://developer.apple.com/documentation/uikit/uiapplicationdelegate/1623111-applicationwillterminate#discussion
                    //WillTerminate is called: in situations where the app is running in the background (not suspended) and the system needs to terminate it for some reason.
                    SentryMauiEventProcessor.InForeground = false;
                });

                lifecycle.OnActivated(application => SentryMauiEventProcessor.InForeground = true);

                lifecycle.DidEnterBackground(application => SentryMauiEventProcessor.InForeground = false);
                lifecycle.OnResignActivation(application => SentryMauiEventProcessor.InForeground = false);
            });
#elif ANDROID
            events.AddAndroid(lifecycle =>
            {
                lifecycle.OnApplicationCreating(application => (application as IPlatformApplication)?.HandleMauiEvents());
                lifecycle.OnDestroy(application => (application as IPlatformApplication)?.HandleMauiEvents(bind: false));

                lifecycle.OnResume(activity => SentryMauiEventProcessor.InForeground = true);
                lifecycle.OnStart(activity => SentryMauiEventProcessor.InForeground = true);

                lifecycle.OnStop(activity => SentryMauiEventProcessor.InForeground = false);
                lifecycle.OnPause(activity => SentryMauiEventProcessor.InForeground = false);
            });
#elif WINDOWS
            events.AddWindows(lifecycle =>
            {
                lifecycle.OnLaunching((application, _) => (application as IPlatformApplication)?.HandleMauiEvents());
                lifecycle.OnClosed((application, _) => (application as IPlatformApplication)?.HandleMauiEvents(bind: false));
            });
#endif
        });
    }

    private static void HandleMauiEvents(this IPlatformApplication platformApplication, bool bind = true)
    {
        // We need to resolve the application manually, because it's not necessarily
        // set on platformApplication.Application at this point in the lifecycle.
        var services = platformApplication.Services;
        var app = services.GetService<IApplication>();

        // Use a real Application control, because the required events needed for binding
        // are not present on IApplication and related interfaces.
        if (app is not Application application)
        {
            var options = services.GetService<IOptions<SentryMauiOptions>>()?.Value;
            options?.LogWarning("Could not bind to MAUI events!");
            return;
        }

        // Bind the events
        var binder = services.GetRequiredService<IMauiEventsBinder>();
        binder.HandleApplicationEvents(application, bind);
    }
}
