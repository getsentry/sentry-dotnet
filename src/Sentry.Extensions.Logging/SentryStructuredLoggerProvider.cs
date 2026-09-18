using Microsoft.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Sentry Logger Provider for <see cref="SentryLog"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal class SentryStructuredLoggerProvider : ILoggerProvider
{
    private readonly IHub _hub;
    private readonly ISystemClock _clock;
    private readonly SdkVersion _sdk;

    public SentryStructuredLoggerProvider(IHub hub)
        : this(hub, SystemClock.Clock, CreateSdkVersion())
    {
    }

    internal SentryStructuredLoggerProvider(IHub hub, ISystemClock clock, SdkVersion sdk)
    {
        _hub = hub;
        _clock = clock;
        _sdk = sdk;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SentryStructuredLogger(categoryName, _hub, _clock, _sdk);
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
