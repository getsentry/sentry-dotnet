namespace Sentry.Internal;

/// <summary>
/// This is a struct-based alternative to <see cref="System.Diagnostics.Stopwatch"/>.
/// It avoids unnecessary allocations and includes realtime clock values.
/// </summary>
internal struct SentryStopwatch
{
    private static readonly double StopwatchTicksPerTimeSpanTick =
        (double)Stopwatch.Frequency / TimeSpan.TicksPerSecond;
    private static readonly double StopwatchTicksPerNs = (double)Stopwatch.Frequency / 1000000000.0;

    private long _startTimestamp;
    private DateTimeOffset _startDateTimeOffset;

    public static SentryStopwatch StartNew() => new()
    {
        _startTimestamp = Stopwatch.GetTimestamp(),
        _startDateTimeOffset = DateTimeOffset.UtcNow
    };

    public DateTimeOffset StartDateTimeOffset => _startDateTimeOffset;
    public DateTimeOffset CurrentDateTimeOffset => _startDateTimeOffset + Elapsed;

    private long Diff() => Stopwatch.GetTimestamp() - _startTimestamp;

    public TimeSpan Elapsed
    {
        get => TimeSpan.FromTicks((long)(Diff() / StopwatchTicksPerTimeSpanTick));
    }

    public ulong ElapsedNanoseconds
    {
        get => (ulong)(Diff() / StopwatchTicksPerNs);
    }
}
