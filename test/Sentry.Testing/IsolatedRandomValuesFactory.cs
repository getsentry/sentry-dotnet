namespace Sentry.Testing;

internal class IsolatedRandomValuesFactory : RandomValuesFactory
{
    private readonly Random _random = new();

    public override int NextInt() => _random.Next();

    public override int NextInt(int minValue, int maxValue) => _random.Next(minValue, maxValue);

    public override double NextDouble() => _random.NextDouble();

    public override void NextBytes(byte[] bytes) => _random.NextBytes(bytes);
#if NET6_0_OR_GREATER
    public override void NextBytes(Span<byte> bytes) => _random.NextBytes(bytes);
#endif

}
