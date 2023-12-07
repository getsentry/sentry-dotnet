namespace Sentry;

internal static class MetricBucketHelper
{
    private const int RollupInSeconds = 10;

    private static readonly DateTime EpochStart = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    internal static long GetTimeBucketKey(this DateTime timestamp)
    {
        var seconds = (long)(timestamp.ToUniversalTime() - EpochStart).TotalSeconds;
        return (seconds / RollupInSeconds) * RollupInSeconds;
    }

    /// <summary>
    /// The aggregator shifts it's flushing by up to an entire rollup window to avoid multiple clients trampling on end
    /// of a 10 second window as all the buckets are anchored to multiples of ROLLUP seconds.  We randomize this number
    /// once per aggregator boot to achieve some level of offsetting across a fleet of deployed SDKs.  Relay itself will
    /// also apply independent jittering.
    /// </summary>
    private static readonly double _flushShift = new Random().NextDouble() * RollupInSeconds;
    internal static DateTime GetCutoff() => DateTime.UtcNow
        .Subtract(TimeSpan.FromSeconds(RollupInSeconds))
        .Subtract(TimeSpan.FromSeconds(_flushShift));
}
