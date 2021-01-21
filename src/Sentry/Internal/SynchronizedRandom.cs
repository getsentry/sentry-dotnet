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
    }
}
