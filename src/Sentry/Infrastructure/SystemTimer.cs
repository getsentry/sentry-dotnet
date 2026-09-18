namespace Sentry.Infrastructure;

/// <summary>
/// Production <see cref="ISentryTimer"/> backed by <see cref="System.Threading.Timer"/>.
/// </summary>
internal sealed class SystemTimer : ISentryTimer
{
    private readonly Timer _timer;

    public SystemTimer(Action callback)
    {
        _timer = new Timer(_ => callback(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Start(TimeSpan timeout) =>
        _timer.Change(timeout, Timeout.InfiniteTimeSpan);

    public void Cancel() =>
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    public void Dispose() => _timer.Dispose();
}
