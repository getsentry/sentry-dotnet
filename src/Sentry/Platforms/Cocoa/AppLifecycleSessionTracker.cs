using Foundation;
using Sentry.Extensibility;
using UIKit;

namespace Sentry.Cocoa;

internal sealed class AppLifecycleSessionTracker : IDisposable
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly List<NSObject> _observers = new();
    private volatile bool _disposed;

    private AppLifecycleSessionTracker(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    public static AppLifecycleSessionTracker Start(IHub hub, SentryOptions options)
    {
        var tracker = new AppLifecycleSessionTracker(hub, options);
        tracker.AddObservers();
        NSRunLoop.Main.BeginInvokeOnMainThread(tracker.PauseIfInBackground);
        return tracker;
    }

    private void AddObservers()
    {
        var center = NSNotificationCenter.DefaultCenter;
        _observers.Add(center.AddObserver(UIApplication.DidEnterBackgroundNotification, _ => Pause()));
        _observers.Add(center.AddObserver(UIApplication.WillTerminateNotification, _ => Pause()));
        _observers.Add(center.AddObserver(UIApplication.WillEnterForegroundNotification, _ => Resume()));
    }

    // Apps launched for a background fetch, a silent push or a location update never move to the background
    private void PauseIfInBackground()
    {
        try
        {
            if (UIApplication.SharedApplication is { ApplicationState: UIApplicationState.Background })
            {
                Pause();
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to read the application state for session tracking.");
        }
    }

    private void Pause()
    {
        if (!_disposed)
        {
            _hub.PauseSession();
        }
    }

    private void Resume()
    {
        if (!_disposed)
        {
            _hub.ResumeSession();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var observer in _observers)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
        }
        _observers.Clear();
    }
}
