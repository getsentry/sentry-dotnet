using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Maui;

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
        options.AddEventProcessor(new MauiEventProcessor());
    }
}
