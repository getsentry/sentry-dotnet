#if NET6_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

// TODO: Re-enable these if we find a solution to https://github.com/dotnet/runtime/discussions/94651
#pragma warning disable SYSLIB1100
#pragma warning disable SYSLIB1101
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
#pragma warning restore SYSLIB1100
#pragma warning restore SYSLIB1101
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
