namespace Sentry.Internal;

internal abstract class RandomValuesFactory
{
    public abstract int NextInt();
    public abstract int NextInt(int minValue, int maxValue);
    public abstract double NextDouble();
    public abstract void NextBytes(byte[] bytes);

#if !(NETSTANDARD2_0 || NET462)
    public abstract void NextBytes(Span<byte> bytes);
#endif

    public bool NextBool(double rate) => rate switch
    {
        >= 1 => true,
        <= 0 => false,
        _ => NextDouble() < rate
    };
}
