using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Maui.Internal;

internal class SentryMauiOptionsSetup : ConfigureFromConfigurationOptions<SentryMauiOptions>
{
    public SentryMauiOptionsSetup(IConfiguration config) : base(config)
    {
    }

    public override void Configure(SentryMauiOptions options)
    {
        base.Configure(options);

        // We'll initialize the SDK in SentryMauiInitializer
        options.InitializeSdk = false;

        // Global Mode makes sense for client apps
        options.IsGlobalModeEnabled = true;

        // We'll use an event processor to set things like SDK name
        options.AddEventProcessor(new SentryMauiEventProcessor(options));

        // Set a default cache path on the device.
        // NOTE: We move the Android SDK's cache path one level below this, in src/Sentry/Android/SentrySdk.cs
        //       We'll want to do something similar when we add iOS support,
        //       but that's blocked by https://github.com/getsentry/sentry-cocoa/issues/1051
        options.CacheDirectoryPath = Path.Combine(FileSystem.CacheDirectory, "sentry");

        // We can use MAUI's network connectivity information to inform the CachingTransport when we're offline.
        options.NetworkStatusListener = new MauiNetworkStatusListener(Connectivity.Current, options);
    }
}
