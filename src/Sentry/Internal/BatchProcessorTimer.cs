using System.Timers;

namespace Sentry.Internal;

internal abstract class BatchProcessorTimer : IDisposable
{
    protected BatchProcessorTimer()
    {
    }

    public abstract bool Enabled { get; set; }

    public abstract event EventHandler<ElapsedEventArgs> Elapsed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}

internal sealed class TimersBatchProcessorTimer : BatchProcessorTimer
{
    private readonly System.Timers.Timer _timer;

    public TimersBatchProcessorTimer(TimeSpan interval)
    {
        _timer = new System.Timers.Timer(interval.TotalMilliseconds)
        {
            AutoReset = false,
            Enabled = false,
        };
        _timer.Elapsed += OnElapsed;
    }

    public override bool Enabled
    {
        get => _timer.Enabled;
        set => _timer.Enabled = value;
    }

    public override event EventHandler<ElapsedEventArgs>? Elapsed;

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        Elapsed?.Invoke(sender, e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Elapsed -= OnElapsed;
            _timer.Dispose();
        }
    }
}
