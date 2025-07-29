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
        : this(options.Value, SystemClock.Clock, hub, CreateSdkVersion())
    {
    }

    internal SentryAspNetCoreStructuredLoggerProvider(SentryLoggingOptions options, ISystemClock clock, IHub hub, SdkVersion sdk)
        : base(options, clock, hub, sdk)
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
