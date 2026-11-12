using Android.App;
using AndroidX.Lifecycle;
using Sentry.Extensibility;

namespace Sentry.Android;

internal sealed class AppLifecycleSessionTracker : IDisposable
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly AndroidHandler _mainHandler = new(AndroidLooper.MainLooper!);
    private readonly Func<bool> _isInForeground;
    private readonly Observer _observer;
    private volatile bool _disposed;

    internal AppLifecycleSessionTracker(IHub hub, SentryOptions options, Func<bool>? isInForeground = null)
    {
        _hub = hub;
        _options = options;
        _isInForeground = isInForeground ?? HasForegroundImportance;
        _observer = new Observer(this);
    }

    public static AppLifecycleSessionTracker Start(IHub hub, SentryOptions options)
    {
        var tracker = new AppLifecycleSessionTracker(hub, options);
        tracker.RunOnMainThread(tracker.AddObserver);
        return tracker;
    }

    private void AddObserver()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(_observer);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to observe the app lifecycle for session tracking.");
        }

        StartSessionIfInForeground();
    }

    internal void StartSessionIfInForeground()
    {
        if (_isInForeground() && !_hub.IsSessionActive)
        {
            _hub.StartSession();
        }
    }

    private bool HasForegroundImportance()
    {
        try
        {
            var processInfo = new ActivityManager.RunningAppProcessInfo();
            ActivityManager.GetMyMemoryState(processInfo);
            return processInfo.Importance == Importance.Foreground;
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to read the process importance for session tracking.");
            return false;
        }
    }

    internal void OnStateChanged(Lifecycle.Event e)
    {
        if (_disposed)
        {
            return;
        }

        if (e == Lifecycle.Event.OnStop)
        {
            _hub.PauseSession();
        }
        else if (e == Lifecycle.Event.OnStart)
        {
            ResumeOrStartSession();
        }
    }

    private void ResumeOrStartSession()
    {
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
        RunOnMainThread(() =>
        {
            try
            {
                ProcessLifecycleOwner.Get().Lifecycle.RemoveObserver(_observer);
            }
            catch (Exception e)
            {
                _options.LogError(e, "Failed to stop observing the app lifecycle.");
            }
        });
    }

    private void RunOnMainThread(Action action)
    {
        if (AndroidLooper.MyLooper() == AndroidLooper.MainLooper)
        {
            action();
        }
        else
        {
            _mainHandler.Post(action);
        }
    }

    private sealed class Observer(AppLifecycleSessionTracker tracker) : JavaObject, ILifecycleEventObserver
    {
        public void OnStateChanged(ILifecycleOwner source, Lifecycle.Event e) => tracker.OnStateChanged(e);
    }
}
