namespace Sentry;

internal static class MetricHelper
{
    private const int RollupInSeconds = 10;

#if NET6_0_OR_GREATER
        static readonly DateTime UnixEpoch = DateTime.UnixEpoch;
#else
    static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
#endif

    internal static long GetDayBucketKey(this DateTime timestamp)
    {
        var utc = timestamp.ToUniversalTime();
        var dayOnly = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, 0, DateTimeKind.Utc);
        return (long)(dayOnly - UnixEpoch).TotalSeconds;
    }

    internal static long GetTimeBucketKey(this DateTime timestamp)
    {
        var seconds = (long)(timestamp.ToUniversalTime() - UnixEpoch).TotalSeconds;
        return (seconds / RollupInSeconds) * RollupInSeconds;
    }

    /// <summary>
    /// The aggregator shifts it's flushing by up to an entire rollup window to avoid multiple clients trampling on end
    /// of a 10 second window as all the buckets are anchored to multiples of ROLLUP seconds.  We randomize this number
    /// once per aggregator boot to achieve some level of offsetting across a fleet of deployed SDKs.  Relay itself will
    /// also apply independent jittering.
    /// </summary>
    /// <remarks>Internal for testing</remarks>
    internal static double FlushShift = new Random().Next(0, 1000) * RollupInSeconds;
    internal static DateTime GetCutoff() => DateTime.UtcNow
        .Subtract(TimeSpan.FromSeconds(RollupInSeconds))
        .Subtract(TimeSpan.FromMilliseconds(FlushShift));

    internal static string SanitizeKey(string input) => Regex.Replace(input, @"[^a-zA-Z0-9_/.-]+", "_", RegexOptions.Compiled);
    internal static string SanitizeValue(string input) => Regex.Replace(input, @"[^\w\d_:/@\.\{\}\[\]$-]+", "_", RegexOptions.Compiled);
}
