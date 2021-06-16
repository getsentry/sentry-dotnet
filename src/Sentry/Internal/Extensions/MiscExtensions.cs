using System;

namespace Sentry.Internal.Extensions
{
    internal static class MiscExtensions
    {
        public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> pipe) => pipe(input);
    }
}
