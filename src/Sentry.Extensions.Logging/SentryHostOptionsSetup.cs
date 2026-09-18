#if NET6_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging;

internal sealed class SentryHostOptionsSetup<TOptions> : IConfigureOptions<TOptions>
    where TOptions : SentryHostOptions
{
    private readonly IConfiguration _config;

    public SentryHostOptionsSetup(ILoggerProviderConfiguration<SentryLoggerProvider> config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Configuration;
    }

    public void Configure(TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var bindable = new BindableSentryHostOptions();
        _config.Bind(bindable);
        bindable.ApplyTo(options);
    }
}
#endif
