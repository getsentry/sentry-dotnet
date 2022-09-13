#if ANDROID
using Java.Util.Concurrent;
#else
using System;
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

#if ANDROID
        // We use the Java implementation because there were device test failures with the .NET one
        // related to uneven distribution when run on Android
        public static int Next(int minValue, int maxValue) => ThreadLocalRandom.Current()!.NextInt(minValue, maxValue);
        public static int Next() => ThreadLocalRandom.Current()!.NextInt();
        public static double NextDouble() => ThreadLocalRandom.Current()!.NextDouble();
        public static void NextBytes(byte[] bytes) => ThreadLocalRandom.Current()!.NextBytes(bytes);
#elif NET6_0_OR_GREATER
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
