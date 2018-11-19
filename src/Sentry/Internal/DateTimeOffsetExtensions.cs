#if NET45
using System;

namespace Sentry.Internal
{
    internal static class DateTimeOffsetExtensions
    {
        private static readonly long _unixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        private static readonly long _unixEpochSeconds = _unixEpochTicks / TimeSpan.TicksPerSecond; // 62,135,596,800

        // https://github.com/dotnet/corefx/blob/664d98b3dc83a56e1e6454591c585cc6a8e19b78/src/Common/src/CoreLib/System/DateTimeOffset.cs#L716
        public static long ToUnixTimeSeconds(this DateTimeOffset date)
        {
            // Truncate sub-second precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times.
            //
            // For example, consider the DateTimeOffset 12/31/1969 12:59:59.001 +0
            //   ticks            = 621355967990010000
            //   ticksFromEpoch   = ticks - DateTime.UnixEpochTicks          = -9990000
            //   secondsFromEpoch = ticksFromEpoch / TimeSpan.TicksPerSecond = 0
            //
            // Notice that secondsFromEpoch is rounded *up* by the truncation induced by integer division,
            // whereas we actually always want to round *down* when converting to Unix time. This happens
            // automatically for positive Unix time values. Now the example becomes:
            //   seconds          = ticks / TimeSpan.TicksPerSecond = 62135596799
            //   secondsFromEpoch = seconds - UnixEpochSeconds      = -1
            //
            // In other words, we want to consistently round toward the time 1/1/0001 00:00:00,
            // rather than toward the Unix Epoch (1/1/1970 00:00:00).
            long seconds = date.Ticks / TimeSpan.TicksPerSecond;
            return seconds - _unixEpochSeconds;
        }
    }
}
#endif
