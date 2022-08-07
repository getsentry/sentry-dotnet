using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Sentry.Internal.Extensions
{
    internal static class MiscExtensions
    {
        public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> pipe) => pipe(input);

        public static T? NullIfDefault<T>(this T value) where T : struct =>
            !EqualityComparer<T>.Default.Equals(value, default)
                ? value
                : null;

        public static string ToHexString(this long l) =>
            "0x" + l.ToString("x", CultureInfo.InvariantCulture);

        private static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        public static void CancelAfterSafe(this CancellationTokenSource cts, TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero)
            {
                // CancelAfter(TimeSpan.Zero) may not cancel immediately, but Cancel always will.
                cts.Cancel();
            }
            else if (timeout > MaxTimeout)
            {
                // Timeout milliseconds can't be larger than int.MaxValue
                // Treat such values (i.e. TimeSpan.MaxValue) as an infinite timeout (-1 ms).
                cts.CancelAfter(Timeout.InfiniteTimeSpan);
            }
            else
            {
                // All other timeout values
                cts.CancelAfter(timeout);
            }
        }
    }
}
