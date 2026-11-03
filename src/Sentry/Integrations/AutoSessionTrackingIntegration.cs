namespace Sentry.Integrations;

internal class AutoSessionTrackingIntegration : ISdkIntegration, IDisposable
{
    private readonly Func<IHub, SentryOptions, IDisposable?> _subscribeToAppLifecycle;
    private IDisposable? _appLifecycleSubscription;

    public AutoSessionTrackingIntegration() : this(SubscribeToAppLifecycle)
    {
    }

    internal AutoSessionTrackingIntegration(Func<IHub, SentryOptions, IDisposable?> subscribeToAppLifecycle)
    {
        _subscribeToAppLifecycle = subscribeToAppLifecycle;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        if (options.AutoSessionTracking)
        {
            hub.StartSession();
            _appLifecycleSubscription = _subscribeToAppLifecycle(hub, options);
        }
    }

    public void Dispose() => _appLifecycleSubscription?.Dispose();

    private static IDisposable? SubscribeToAppLifecycle(IHub hub, SentryOptions options) =>
#if ANDROID
        Sentry.Android.AppLifecycleSessionTracker.Start(hub, options);
#elif IOS || MACCATALYST
        Sentry.Cocoa.AppLifecycleSessionTracker.Start(hub, options);
#else
        null;
#endif
}
