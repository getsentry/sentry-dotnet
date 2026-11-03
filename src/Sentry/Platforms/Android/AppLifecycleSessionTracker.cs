using AndroidX.Lifecycle;
using Sentry.Extensibility;

namespace Sentry.Android;

internal sealed class AppLifecycleSessionTracker : IDisposable
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly AndroidHandler _mainHandler = new(AndroidLooper.MainLooper!);
    private readonly Observer _observer;
    private volatile bool _disposed;

    private AppLifecycleSessionTracker(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
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
            var lifecycle = ProcessLifecycleOwner.Get().Lifecycle;

            // Processes started for a push, a broadcast or a job never move to the background
            if (!lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!))
            {
                _hub.PauseSession();
            }

            lifecycle.AddObserver(_observer);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to observe the app lifecycle for session tracking.");
        }
    }

    private void OnStateChanged(Lifecycle.Event e)
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
