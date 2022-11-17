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

        // NOTE: Anything set here will overwrite options set by the user.
        //       For option defaults that can be changed, use the constructor in SentryMauiOptions instead.

        // Always initialize the SDK (via Sentry.Extensions.Logging)
        options.InitializeSdk = true;

        // Global Mode makes sense for client apps
        options.IsGlobalModeEnabled = true;

        // We'll use an event processor to set things like SDK name
        options.AddEventProcessor(new SentryMauiEventProcessor(options));

#if !PLATFORM_NEUTRAL
        // We can use MAUI's network connectivity information to inform the CachingTransport when we're offline.
        options.NetworkStatusListener = new MauiNetworkStatusListener(Connectivity.Current, options);
#endif
    }
}
