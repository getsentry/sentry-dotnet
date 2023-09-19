namespace Sentry.Internal;

internal class SynchronizedRandomValuesFactory : RandomValuesFactory
{
#if NET6_0_OR_GREATER
        public override int NextInt() => Random.Shared.Next();
        public override int NextInt(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
        public override double NextDouble() => Random.Shared.NextDouble();
        public override void NextBytes(byte[] bytes) => Random.Shared.NextBytes(bytes);
        public override void NextBytes(Span<byte> bytes) => Random.Shared.NextBytes(bytes);
#else
    private static readonly AsyncLocal<Random> LocalRandom = new();
    private static Random Random => LocalRandom.Value ??= new Random();

    public override int NextInt() => Random.Next();
    public override int NextInt(int minValue, int maxValue) => Random.Next(minValue, maxValue);
    public override double NextDouble() => Random.NextDouble();
    public override void NextBytes(byte[] bytes) => Random.NextBytes(bytes);

#if !(NETSTANDARD2_0 || NET461)
    public override void NextBytes(Span<byte> bytes) => Random.NextBytes(bytes);
#endif

#endif
}
