using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.AspNetCore;

/// <summary>
/// Sentry Logger Provider for <see cref="SentryLog"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal sealed class SentryAspNetCoreStructuredLoggerProvider : SentryStructuredLoggerProvider
{
    public SentryAspNetCoreStructuredLoggerProvider(IHub hub)
        : this(hub, SystemClock.Clock)
    {
    }

    internal SentryAspNetCoreStructuredLoggerProvider(IHub hub, ISystemClock clock)
        : base(hub, clock)
    {
    }
}
