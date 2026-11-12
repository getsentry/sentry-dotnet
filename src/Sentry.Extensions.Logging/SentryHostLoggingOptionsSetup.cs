using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

internal sealed class SentryHostLoggingOptionsSetup<TOptions> : IConfigureOptions<TOptions>
    where TOptions : SentryHostOptions
{
    private readonly IConfiguration _loggingSection;

    public SentryHostLoggingOptionsSetup(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _loggingSection = configuration.GetSection("Logging:Sentry");
    }

    public void Configure(TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        SentryLoggingConfiguration.ApplyTo(_loggingSection, options.Logging);
    }
}
