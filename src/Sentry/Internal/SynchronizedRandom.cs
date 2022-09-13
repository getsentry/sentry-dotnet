using System;

#if !NET6_0_OR_GREATER
using System.Threading;
#endif

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
        private static readonly AsyncLocal<Random> LocalRandom = new();
        private static Random Random => LocalRandom.Value ??= new Random();

        public static void NextBytes(byte[] bytes) => Random.NextBytes(bytes);
        public static double NextDouble() => Random.NextDouble();
        public static int Next(int minValue, int maxValue) => Random.Next(minValue, maxValue);
        public static int Next() => Random.Next();
#endif
    }
}
