using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.Maui.Internal;

/// <summary>
/// Sentry Logger Provider for <see cref="Breadcrumb"/> and <see cref="SentryEvent"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal sealed class SentryMauiLoggerProvider : SentryLoggerProvider
{
    public SentryMauiLoggerProvider(IOptions<SentryMauiOptions> options, IHub hub)
        : base(options, hub)
    {
    }

    internal SentryMauiLoggerProvider(SentryMauiOptions options, IHub hub, ISystemClock clock)
        : base(hub, clock, options)
    {
    }
}
