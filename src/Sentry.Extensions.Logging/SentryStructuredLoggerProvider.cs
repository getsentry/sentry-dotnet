using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Infrastructure;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Sentry Logger Provider for <see cref="SentryLog"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal class SentryStructuredLoggerProvider : ILoggerProvider
{
    private readonly SentryLoggingOptions _options;
    private readonly IHub _hub;
    private readonly ISystemClock _clock;
    private readonly SdkVersion _sdk;

    public SentryStructuredLoggerProvider(IOptions<SentryLoggingOptions> options, IHub hub)
        : this(options.Value, hub, SystemClock.Clock, CreateSdkVersion())
    {
    }

    internal SentryStructuredLoggerProvider(IHub hub, ISystemClock clock, SentryLoggingOptions options)
        : this(options, hub, clock, CreateSdkVersion())
    {
    }

    internal SentryStructuredLoggerProvider(SentryLoggingOptions options, IHub hub, ISystemClock clock, SdkVersion sdk)
    {
        _options = options;
        _hub = hub;
        _clock = clock;
        _sdk = sdk;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SentryStructuredLogger(categoryName, _options, _hub, _clock, _sdk);
    }

    public void Dispose()
    {
    }

    private static SdkVersion CreateSdkVersion()
    {
        return new SdkVersion
        {
            Name = Constants.SdkName,
            Version = SentryLoggerProvider.NameAndVersion.Version,
        };
    }
}
