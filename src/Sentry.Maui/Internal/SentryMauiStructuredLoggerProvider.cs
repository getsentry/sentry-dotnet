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
        : this(hub, SystemClock.Clock, CreateSdkVersion())
    {
    }

    internal SentryMauiStructuredLoggerProvider(IHub hub, ISystemClock clock, SdkVersion sdk)
        : base(hub, clock, sdk)
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
