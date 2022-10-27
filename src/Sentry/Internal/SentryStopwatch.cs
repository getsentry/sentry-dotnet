using System;
using System.Diagnostics;

namespace Sentry.Internal;

/// <summary>
/// This is a struct-based alternative to <see cref="System.Diagnostics.Stopwatch"/>.
/// It avoids unnecessary allocations and includes realtime clock values.
/// </summary>
internal struct SentryStopwatch
{
    private static readonly double StopwatchTicksPerTimeSpanTick =
        (double)Stopwatch.Frequency / TimeSpan.TicksPerSecond;

    private long _startTimestamp;
    private DateTimeOffset _startDateTimeOffset;

    public static SentryStopwatch StartNew() => new()
    {
        _startTimestamp = Stopwatch.GetTimestamp(),
        _startDateTimeOffset = DateTimeOffset.UtcNow
    };

    public DateTimeOffset StartDateTimeOffset => _startDateTimeOffset;
    public DateTimeOffset CurrentDateTimeOffset => _startDateTimeOffset + Elapsed;

    public TimeSpan Elapsed
    {
        get
        {
            var now = Stopwatch.GetTimestamp();
            var diff = now - _startTimestamp;
            var ticks = (long)(diff / StopwatchTicksPerTimeSpanTick);
            return TimeSpan.FromTicks(ticks);
        }
    }
}
