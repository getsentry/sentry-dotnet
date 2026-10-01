using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

internal sealed class SentryLoggingOptionsSetup : IConfigureOptions<SentryLoggingOptions>
{
    private readonly IConfiguration _config;

    public SentryLoggingOptionsSetup(ILoggerProviderConfiguration<SentryLoggerProvider> config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Configuration;
    }

    public void Configure(SentryLoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        SentryLoggingConfiguration.ApplyTo(_config, options);
    }
}
