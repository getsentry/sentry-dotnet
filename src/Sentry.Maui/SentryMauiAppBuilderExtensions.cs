using Microsoft.Extensions.Configuration;
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

        var section = builder.Configuration.GetSection("Sentry");
        services.AddSingleton<IConfigureOptions<SentryMauiOptions>>(_ => new SentryMauiOptionsSetup(section));

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
            events.AddiOS(lifecycle => lifecycle.WillFinishLaunching((application, launchOptions) =>
            {
                // A bit of hackery here, because we can't mock UIKit.UIApplication in tests.
                var platformApplication = application != null!
                    ? application.Delegate as IPlatformApplication
                    : launchOptions["application"] as IPlatformApplication;

                platformApplication?.BindMauiEvents();
                return true;
            }));
#elif ANDROID
            events.AddAndroid(lifecycle => lifecycle.OnApplicationCreating(application =>
                (application as IPlatformApplication)?.BindMauiEvents()));
#elif WINDOWS
            events.AddWindows(lifecycle =>
            {
                lifecycle.OnLaunching((application, _) =>
                {
                    (application as IPlatformApplication)?.BindMauiEvents();
                });
                lifecycle.OnClosed((application, _) =>
                {
                    (application as IPlatformApplication)?.UnbindMauiEvents();
                });
            });
#endif
        });
    }

    private static void BindMauiEvents(this IPlatformApplication platformApplication)
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
        binder.BindApplicationEvents(application);
    }

    private static void UnbindMauiEvents(this IPlatformApplication platformApplication)
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
            options?.LogWarning("Could not unbind from MAUI events!");
            return;
        }

        // Unbind the events
        var binder = services.GetRequiredService<IMauiEventsBinder>();
        binder.UnbindApplicationEvents(application);
    }
}
