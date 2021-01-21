using System;

namespace Sentry.Internal
{
    internal static class SynchronizedRandom
    {
        private static readonly Lazy<Random> RandomLazy = new();

        private static Random Random => RandomLazy.Value;

        public static double NextDouble()
        {
            lock (RandomLazy)
            {
                return Random.NextDouble();
            }
        }
    }
}
