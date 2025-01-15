using Sentry.Integrations;

namespace Sentry.Maui.Internal;

internal class SentryMauiReplayIntegration : ISdkIntegration
{
    private readonly SentryMauiOptions _sentryMauiOptions;

    public SentryMauiReplayIntegration(SentryMauiOptions sentryMauiOptions)
    {
        ArgumentNullException.ThrowIfNull(sentryMauiOptions);

        _sentryMauiOptions = sentryMauiOptions;
    }

    public void Register(IHub hub, SentryOptions options)
    {
#if IOS || MACCATALYST
        if (_sentryMauiOptions.SessionReplayEnabled)
        {
            var breadcrumbConverter = new ReplayBreadcrumbConverter();
            Sentry.CocoaSdk.PrivateSentrySDKOnly.ConfigureSessionReplayWith(breadcrumbConverter, null);
        }
#elif ANDROID

#endif
    }
}
