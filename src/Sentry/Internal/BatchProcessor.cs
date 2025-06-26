using System.Timers;
using Sentry.Protocol.Envelopes;

#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = object;
#endif

namespace Sentry.Internal;

internal sealed class BatchProcessor : IDisposable
{
    private readonly IHub _hub;
    private readonly System.Timers.Timer _timer;
    private readonly BatchBuffer<SentryLog> _logs;
    private readonly Lock _lock;

    private DateTime _lastFlush = DateTime.MinValue;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval)
    {
        _hub = hub;

        _timer = new System.Timers.Timer(batchInterval.TotalMilliseconds)
        {
            AutoReset = false,
            Enabled = false,
        };
        _timer.Elapsed += IntervalElapsed;

        _logs = new BatchBuffer<SentryLog>(batchCount);
        _lock = new Lock();
    }

    internal void Enqueue(SentryLog log)
    {
        lock (_lock)
        {
            EnqueueCore(log);
        }
    }

    private void EnqueueCore(SentryLog log)
    {
        var empty = _logs.IsEmpty;
        var added = _logs.TryAdd(log);
        Debug.Assert(added, $"Since we currently have no lock-free scenario, it's unexpected to exceed the {nameof(BatchBuffer<SentryLog>)}'s capacity.");

        if (empty && !_logs.IsFull)
        {
            _timer.Enabled = true;
        }
        else if (_logs.IsFull)
        {
            _timer.Enabled = false;
            Flush();
        }
    }

    private void Flush()
    {
        _lastFlush = DateTime.UtcNow;

        var logs = _logs.ToArrayAndClear();
        _ = _hub.CaptureEnvelope(Envelope.FromLogs(logs));
    }

    private void IntervalElapsed(object? sender, ElapsedEventArgs e)
    {
        _timer.Enabled = false;

        lock (_lock)
        {
            if (!_logs.IsEmpty && e.SignalTime > _lastFlush)
            {
                Flush();
            }
        }
    }

    public void Dispose()
    {
        _timer.Enabled = false;
        _timer.Elapsed -= IntervalElapsed;
        _timer.Dispose();
    }
}
