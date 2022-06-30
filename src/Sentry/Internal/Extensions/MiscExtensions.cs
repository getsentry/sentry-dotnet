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

        public static TimeSpan AdjustForMaxTimeout(this TimeSpan timeout) =>
            timeout.TotalMilliseconds > int.MaxValue ? Timeout.InfiniteTimeSpan : timeout;
    }
}
