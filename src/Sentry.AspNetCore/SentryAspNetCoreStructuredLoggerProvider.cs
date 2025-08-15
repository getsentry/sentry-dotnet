using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.AspNetCore;

/// <summary>
/// Structured Logger Provider for Sentry.
/// </summary>
[ProviderAlias("SentryLogs")]
internal sealed class SentryAspNetCoreStructuredLoggerProvider : SentryStructuredLoggerProvider
{
    public SentryAspNetCoreStructuredLoggerProvider(IOptions<SentryAspNetCoreOptions> options, IHub hub)
        : this(options.Value, hub, SystemClock.Clock, CreateSdkVersion())
    {
    }

    internal SentryAspNetCoreStructuredLoggerProvider(SentryAspNetCoreOptions options, IHub hub, ISystemClock clock, SdkVersion sdk)
        : base(options, hub, clock, sdk)
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
