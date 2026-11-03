using AndroidX.Lifecycle;
using Sentry.Extensibility;

namespace Sentry.Android;

internal sealed class AppLifecycleSessionTracker : IDisposable
{
    // Longer than a cold start, so only processes started for a push, a broadcast or a job get paused
    internal static readonly TimeSpan BackgroundLaunchCheckDelay = TimeSpan.FromSeconds(5);

    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly AndroidHandler _mainHandler = new(AndroidLooper.MainLooper!);
    private readonly Observer _observer;
    private volatile bool _disposed;

    internal AppLifecycleSessionTracker(IHub hub, SentryOptions options)
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
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(_observer);
            _mainHandler.PostDelayed(PauseIfInBackground, (long)BackgroundLaunchCheckDelay.TotalMilliseconds);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to observe the app lifecycle for session tracking.");
        }
    }

    private void PauseIfInBackground()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!ProcessLifecycleOwner.Get().Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!))
            {
                _hub.PauseSession();
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to read the app lifecycle state for session tracking.");
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
