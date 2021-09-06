using System;
using System.Collections.Generic;

namespace Sentry.Internal.Extensions
{
    internal static class MiscExtensions
    {
        public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> pipe) => pipe(input);

        public static T? NullIfDefault<T>(this T value) where T : struct =>
            !EqualityComparer<T>.Default.Equals(value, default)
                ? value
                : null;
    }
}
