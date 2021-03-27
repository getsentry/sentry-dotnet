using System;

namespace Sentry.Internal
{
    internal static class SynchronizedRandom
    {
        private static readonly Random Random = new();

        public static double NextDouble()
        {
            lock (Random)
            {
                return Random.NextDouble();
            }
        }

        public static bool NextBool(double rate) => rate switch
        {
            >= 1 => true,
            <= 0 => false,
            _ => NextDouble() < rate
        };
    }
}
