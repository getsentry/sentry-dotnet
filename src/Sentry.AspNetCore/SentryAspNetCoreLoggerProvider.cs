using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.AspNetCore;

/// <summary>
/// Sentry Logger Provider for <see cref="Breadcrumb"/> and <see cref="SentryEvent"/>.
/// </summary>
[ProviderAlias("Sentry")]
internal sealed class SentryAspNetCoreLoggerProvider : SentryLoggerProvider
{
    /// <summary>
    /// Creates a new instance of <see cref="SentryAspNetCoreLoggerProvider"/>
    /// </summary>
    public SentryAspNetCoreLoggerProvider(IOptions<SentryAspNetCoreOptions> options, IHub hub)
        : base(options, hub)
    {
    }

    internal SentryAspNetCoreLoggerProvider(SentryAspNetCoreOptions options, IHub hub, ISystemClock clock)
        : base(hub, clock, options)
    {
    }
}
