using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Maui.Internal;

internal class SentryMauiConfigurationOptionsSetup : IConfigureOptions<SentryMauiOptions>
{
    private readonly IConfiguration _config;

    public SentryMauiConfigurationOptionsSetup(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.GetSection("Sentry");
    }

    public void Configure(SentryMauiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var bindable = new BindableSentryMauiOptions();
        _config.Bind(bindable);
        bindable.ApplyTo(options);
    }
}
