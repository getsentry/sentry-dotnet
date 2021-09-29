using System;

namespace Sentry.Internal
{
    internal static class SynchronizedRandom
    {
        public static bool NextBool(double rate) => rate switch
        {
            >= 1 => true,
            <= 0 => false,
            _ => NextDouble() < rate
        };

#if NET6_0_OR_GREATER
        public static double NextDouble() => Random.Shared.NextDouble();
#else
        private static readonly Random Random = new();

        public static double NextDouble()
        {
            lock (Random)
            {
                return Random.NextDouble();
            }
        }
#endif
    }
}
