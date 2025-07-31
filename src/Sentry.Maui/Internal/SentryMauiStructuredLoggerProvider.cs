using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.Maui.Internal;

[ProviderAlias("SentryLogs")]
internal sealed class SentryMauiStructuredLoggerProvider : SentryStructuredLoggerProvider
{
    public SentryMauiStructuredLoggerProvider(IOptions<SentryMauiOptions> options, IHub hub)
        : this(options.Value, hub, SystemClock.Clock, CreateSdkVersion())
    {
    }

    internal SentryMauiStructuredLoggerProvider(SentryMauiOptions options, IHub hub, ISystemClock clock, SdkVersion sdk)
        : base(options, hub, clock, sdk)
    {
    }

    private static SdkVersion CreateSdkVersion()
    {
        return new SdkVersion
        {
            Name = Constants.SdkName,
            Version = Constants.SdkVersion,
        };
    }
}
