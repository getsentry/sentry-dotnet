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
            Observe(ProcessLifecycleOwner.Get().Lifecycle);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to observe the app lifecycle for session tracking.");
            _hub.StartSession();
        }
    }

    internal void Observe(Lifecycle lifecycle)
    {
        lifecycle.AddObserver(_observer);
        if (!lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Created!))
        {
            _options.LogWarning("ProcessLifecycleOwner is not initialized, so sessions will not pause in the background.");
            _hub.StartSession();
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
