using System;

namespace Sentry.Internal
{
    internal static class ThreadSafeRandom
    {
        private static readonly object Lock = new();
        private static readonly Lazy<Random> RandomLazy = new();

        private static Random Random => RandomLazy.Value;

        public static double NextDouble()
        {
            lock (Lock)
            {
                return Random.NextDouble();
            }
        }
    }
}
