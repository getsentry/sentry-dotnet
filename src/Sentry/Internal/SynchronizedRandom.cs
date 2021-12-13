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
        public static int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
        public static int Next() => Random.Shared.Next();
        public static double NextDouble() => Random.Shared.NextDouble();
        public static void NextBytes(byte[] bytes) => Random.Shared.NextBytes(bytes);
#else
        // TODO: ThreadLocal instance would avoid contention
        private static readonly Random Random = new();

        public static void NextBytes(byte[] bytes)
        {
            lock (Random)
            {
                Random.NextBytes(bytes);
            }
        }

        public static double NextDouble()
        {
            lock (Random)
            {
                return Random.NextDouble();
            }
        }

        public static int Next(int minValue, int maxValue)
        {
            lock (Random)
            {
                return Random.Next(minValue, maxValue);
            }
        }

        public static int Next()
        {
            lock (Random)
            {
                return Random.Next();
            }
        }
#endif
    }
}
