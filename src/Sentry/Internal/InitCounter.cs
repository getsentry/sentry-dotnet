namespace Sentry.Internal;

/// <summary>
/// This is used internally to track the number of times the SDK has been initialised so that different
/// Hub instances get a different cache directory path, even if they're running in the same process.
/// </summary>
internal interface IInitCounter
{
    public int Count { get; }
    public void Increment();
}

/// <inheritdoc cref="IInitCounter"/>
internal class InitCounter : IInitCounter
{
    internal static InitCounter Instance { get; } = new();

    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);
}
