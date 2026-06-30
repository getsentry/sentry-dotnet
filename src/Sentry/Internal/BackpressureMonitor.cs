using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Internal;

/// <summary>
/// <para>
/// Monitors system health and calculates a DownsampleFactor that can be applied to events and transactions when the
/// system is under load.
/// </para>
/// <para>
/// The health checks used by the monitor are:
/// </para>
/// <list type="number">
///     <item>if any events have been dropped due to queue being full in the last 2 seconds</item>
///     <item>if any new rate limits have been applied since the last check</item>
/// </list>
/// This check is performed every 10 seconds. With each negative health check we halve tracesSampleRate up to 10 times, meaning the original tracesSampleRate is multiplied by 1, 1/2, 1/4, ... up to 1/1024 (~ 0.001%). Any positive health check resets to the original tracesSampleRate set in SentryOptions.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/traces/backpressure/">Backpressure Management</seealso>
internal class BackpressureMonitor : IDisposable
{
    internal const int MaxDownsamples = 10;
    private const int CheckIntervalInSeconds = 10;
    private const int RecentThresholdInSeconds = 2;

    private readonly IDiagnosticLogger? _logger;
    private readonly ISystemClock _clock;
    private long _lastQueueOverflow = DateTimeOffset.MinValue.Ticks;
    private long _lastRateLimitEvent = DateTimeOffset.MinValue.Ticks;
    private volatile int _downsampleLevel = 0;

    private static readonly long RecencyThresholdTicks = TimeSpan.FromSeconds(RecentThresholdInSeconds).Ticks;
    private static readonly long CheckIntervalTicks = TimeSpan.FromSeconds(CheckIntervalInSeconds).Ticks;

    private readonly CancellationTokenSource _cts = new();

    private readonly Task _workerTask;
    internal Task WorkerTask => _workerTask;
    internal int DownsampleLevel => _downsampleLevel;
    internal long LastQueueOverflowTicks => Interlocked.Read(ref _lastQueueOverflow);
    internal long LastRateLimitEventTicks => Interlocked.Read(ref _lastRateLimitEvent);

    public BackpressureMonitor(IDiagnosticLogger? logger, ISystemClock? clock = null, bool enablePeriodicHealthCheck = true,
        TaskScheduler? scheduler = null)
    {
        _logger = logger;
        _clock = clock ?? SystemClock.Clock;

        if (enablePeriodicHealthCheck)
        {
            _logger?.LogDebug("Starting BackpressureMonitor.");
            // Default to the thread pool. The scheduler is only injected by tests, to model single-threaded
            // runtimes (e.g. Unity WebGL) where the worker and its continuations run on the same thread.
            _workerTask = scheduler is null
                ? Task.Run(() => DoWorkAsync(_cts.Token))
                : Task.Factory.StartNew(() => DoWorkAsync(_cts.Token), _cts.Token, TaskCreationOptions.None, scheduler)
                    .Unwrap();
        }
        else
        {
            _workerTask = Task.CompletedTask;
        }
    }

    /// <summary>
    /// For testing purposes only. Sets the downsample level directly.
    /// </summary>
    internal void SetDownsampleLevel(int level)
    {
        Interlocked.Exchange(ref _downsampleLevel, level);
    }

    internal void IncrementDownsampleLevel()
    {
        var oldValue = _downsampleLevel;
        if (oldValue < MaxDownsamples)
        {
            var newValue = oldValue + 1;
            if (Interlocked.CompareExchange(ref _downsampleLevel, newValue, oldValue) == oldValue)
            {
                _logger?.LogDebug("System is under pressure, increasing downsample level to {0}.", newValue);
            }
        }
    }

    /// <summary>
    /// A multiplier that can be applied to the SampleRate or TracesSampleRate to reduce the amount of data sent to
    /// Sentry when the system is under pressure.
    /// </summary>
    public double DownsampleFactor
    {
        get
        {
            var level = _downsampleLevel;
            return 1d / (1 << level); // 1 / (2^level) = 1, 1/2, 1/4, 1/8, ...
        }
    }

    public void RecordRateLimitHit(DateTimeOffset when) => Interlocked.Exchange(ref _lastRateLimitEvent, when.Ticks);

    public void RecordQueueOverflow() => Interlocked.Exchange(ref _lastQueueOverflow, _clock.GetUtcNow().Ticks);

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DoHealthCheck();

                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalInSeconds), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Task was cancelled, exit gracefully
        }
    }

    internal void DoHealthCheck()
    {
        if (IsHealthy)
        {
            var previous = Interlocked.Exchange(ref _downsampleLevel, 0);
            if (previous > 0)
            {
                _logger?.LogDebug("System is healthy, resetting downsample level.");
            }
        }
        else
        {
            IncrementDownsampleLevel();
        }
    }

    /// <summary>
    /// Checks for any recent queue overflows or any rate limit events since the last check.
    /// </summary>
    /// <returns></returns>
    internal bool IsHealthy
    {
        get
        {
            var nowTicks = _clock.GetUtcNow().Ticks;
            var recentOverflowCutoff = nowTicks - RecencyThresholdTicks;
            var rateLimitCutoff = nowTicks - CheckIntervalTicks;
            return LastQueueOverflowTicks < recentOverflowCutoff && LastRateLimitEventTicks < rateLimitCutoff;
        }
    }

    public void Dispose()
    {
        try
        {
            // Request cancellation but do NOT block on _workerTask here. On single-threaded runtimes
            // (e.g. Unity WebGL / browser-wasm) the worker's cancellation continuation can only be scheduled
            // on the calling thread, so a synchronous _workerTask.Wait() would block the only thread that
            // could complete it - a deadlock. The worker observes the token, exits its Task.Delay loop and
            // unwinds on its own; it produces no result we need to await, and the methods callers may still
            // invoke after disposal (GetDownsampleFactor / RecordQueueOverflow / RecordRateLimitHit) don't
            // touch the cancellation token source. See https://github.com/getsentry/sentry-dotnet/issues/5237
            _cts.Cancel();
        }
        catch (Exception ex)
        {
            // Log rather than throw
            _logger?.LogWarning(ex, "Error in BackpressureMonitor.Dispose");
        }
        finally
        {
            // Dispose the CancellationTokenSource only once the worker has stopped using the token, but
            // without blocking the calling thread. Disposing it inline would race the worker: if it ran while
            // the worker was registering its Task.Delay continuation, the worker could observe an
            // ObjectDisposedException - which it doesn't catch - and fault with an unobserved task exception.
            _workerTask.ContinueWith(
                static (_, state) => ((CancellationTokenSource)state!).Dispose(),
                _cts,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
