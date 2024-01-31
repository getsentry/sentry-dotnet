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

    public void Configure(SentryLoggingOptions options)
    {
        // ArgumentNullException.ThrowIfNull(options);

        // var bindable = new BindableSentryLoggingOptions();
        // _config.Bind(bindable);
        // bindable.ApplyTo(options);
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
