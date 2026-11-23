using Foundation;
using Sentry.Extensibility;
using UIKit;

namespace Sentry.Cocoa;

internal sealed class AppLifecycleSessionTracker : IDisposable
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly NSNotificationCenter _center;
    private readonly List<NSObject> _observers = new();
    private volatile bool _disposed;

    internal AppLifecycleSessionTracker(IHub hub, SentryOptions options, NSNotificationCenter? center = null)
    {
        _hub = hub;
        _options = options;
        _center = center ?? NSNotificationCenter.DefaultCenter;
    }

    public static AppLifecycleSessionTracker Start(IHub hub, SentryOptions options)
    {
        var tracker = new AppLifecycleSessionTracker(hub, options);
        tracker.AddObservers();
        NSRunLoop.Main.BeginInvokeOnMainThread(tracker.StartIfAlreadyActive);
        return tracker;
    }

    internal void AddObservers()
    {
        _observers.Add(_center.AddObserver(UIApplication.DidEnterBackgroundNotification, _ => Pause()));
        _observers.Add(_center.AddObserver(UIApplication.WillTerminateNotification, _ => Pause()));
        _observers.Add(_center.AddObserver(UIApplication.WillEnterForegroundNotification, _ => ResumeOrStart()));
        // A background launch that the user then opens may get DidBecomeActive without WillEnterForeground
        _observers.Add(_center.AddObserver(UIApplication.DidBecomeActiveNotification, _ => ResumeOrStart()));
    }

    // DidBecomeActive has already been posted when the SDK is initialized late
    private void StartIfAlreadyActive()
    {
        try
        {
            if (UIApplication.SharedApplication is { ApplicationState: UIApplicationState.Active })
            {
                ResumeOrStart();
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to read the application state for session tracking.");
        }
    }

    private void Pause()
    {
        if (!_disposed && _hub.IsSessionActive)
        {
            _hub.PauseSession();
        }
    }

    private void ResumeOrStart()
    {
        if (_disposed)
        {
            return;
        }

        if (_hub.IsSessionActive)
        {
            _hub.ResumeSession();
        }
        else
        {
            _hub.StartSession();
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
            _center.RemoveObserver(observer);
        }
        _observers.Clear();
    }
}
