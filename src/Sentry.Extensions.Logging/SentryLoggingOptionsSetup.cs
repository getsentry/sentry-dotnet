#if NET6_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

internal class SentryLoggingOptionsSetup : IConfigureOptions<SentryLoggingOptions>
{
    private readonly IConfiguration _config;

    public SentryLoggingOptionsSetup(ILoggerProviderConfiguration<SentryLoggerProvider> config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Configuration;
    }

    public virtual void Configure(SentryLoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _config.Bind(options);
    }
}
#else
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

internal class SentryLoggingOptionsSetup : ConfigureFromConfigurationOptions<SentryLoggingOptions>
{
    public SentryLoggingOptionsSetup(
        ILoggerProviderConfiguration<SentryLoggerProvider> providerConfiguration)
        : base(providerConfiguration.Configuration)
    { }
}
#endif
