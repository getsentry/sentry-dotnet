using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
#if !PLATFORM_NEUTRAL
using Microsoft.Maui.Networking;
#endif
using Sentry.Infrastructure;

namespace Sentry.Maui.Internal;

internal class SentryMauiOptionsSetup : IConfigureOptions<SentryMauiOptions>
{
    private readonly IConfiguration _config;

    public SentryMauiOptionsSetup(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public void Configure(SentryMauiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var bindable = new BindableSentryMauiOptions();
        _config.Bind(bindable);
        bindable.ApplyTo(options);

#if __ANDROID__ || __IOS__
        options.Native.AttachScreenshot = options.AttachScreenshot;
#endif

        // NOTE: Anything set here will overwrite options set by the user.
        //       For option defaults that can be changed, use the constructor in SentryMauiOptions instead.

        // We'll initialize the SDK in SentryMauiInitializer
        options.InitializeSdk = false;

        // Global Mode makes sense for client apps
        options.IsGlobalModeEnabled = true;

        // So debug logs are visible in both Rider and Visual Studio
        if (options is { Debug: true, DiagnosticLogger: null })
        {
            options.DiagnosticLogger = new ConsoleAndTraceDiagnosticLogger(options.DiagnosticLevel);
        }

        // We'll use an event processor to set things like SDK name
        options.AddEventProcessor(new SentryMauiEventProcessor(options));

        if (options.AttachScreenshot)
        {
            options.AddEventProcessor(new SentryMauiScreenshotProcessor(options));
        }

#if !PLATFORM_NEUTRAL
        // We can use MAUI's network connectivity information to inform the CachingTransport when we're offline.
        options.NetworkStatusListener = new MauiNetworkStatusListener(Connectivity.Current, options);
#endif
    }
}
