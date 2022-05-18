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

        // TODO: Anything MAUI specific for setting up the options. (Can inject dependencies.)
    }
}
