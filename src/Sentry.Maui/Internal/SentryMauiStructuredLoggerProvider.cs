using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.Maui.Internal;

/// <summary>
/// Sentry Logger Provider for <see cref="SentryLog"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal sealed class SentryMauiStructuredLoggerProvider : SentryStructuredLoggerProvider
{
    public SentryMauiStructuredLoggerProvider(IHub hub)
        : this(hub, SystemClock.Clock)
    {
    }

    internal SentryMauiStructuredLoggerProvider(IHub hub, ISystemClock clock)
        : base(hub, clock)
    {
    }
}
