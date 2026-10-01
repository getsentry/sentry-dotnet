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
        : this(hub, SystemClock.Clock, CreateSdkVersion())
    {
    }

    internal SentryAspNetCoreStructuredLoggerProvider(IHub hub, ISystemClock clock, SdkVersion sdk)
        : base(hub, clock, sdk)
    {
    }

    private static SdkVersion CreateSdkVersion()
    {
        return new SdkVersion
        {
            Name = Constants.SdkName,
            Version = SentryMiddleware.NameAndVersion.Version,
        };
    }
}
