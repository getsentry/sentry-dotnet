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

    public SentryStructuredLoggerProvider(IHub hub)
        : this(hub, SystemClock.Clock)
    {
    }

    internal SentryStructuredLoggerProvider(IHub hub, ISystemClock clock)
    {
        _hub = hub;
        _clock = clock;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SentryStructuredLogger(categoryName, _hub, _clock);
    }

    public void Dispose()
    {
    }
}
