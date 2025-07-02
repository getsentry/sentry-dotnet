namespace Sentry.Internal;

/// <summary>
/// A slim wrapper over an <see cref="System.Array"/>,
/// intended for buffering.
/// </summary>
/// <remarks>
/// <para><see cref="Capacity"/> is thread-safe.</para>
/// <para><see cref="TryAdd(T, out int)"/> is thread-safe.</para>
/// <para><see cref="ToArrayAndClear()"/> is not thread-safe.</para>
/// <para><see cref="ToArrayAndClear(int)"/> is not thread-safe.</para>
/// </remarks>
internal sealed class BatchBuffer<T>
{
    private readonly T[] _array;
    private int _additions;

    public BatchBuffer(int capacity)
    {
        ThrowIfLessThanTwo(capacity, nameof(capacity));

        _array = new T[capacity];
        _additions = 0;
    }

    //internal int Count => _count;
    internal int Capacity => _array.Length;
    internal bool IsEmpty => _additions == 0;
    internal bool IsFull => _additions >= _array.Length;

    internal bool TryAdd(T item, out int count)
    {
        count = Interlocked.Increment(ref _additions);

        if (count <= _array.Length)
        {
            _array[count - 1] = item;
            return true;
        }

        return false;
    }

    internal T[] ToArrayAndClear()
    {
        return ToArrayAndClear(_additions);
    }

    internal T[] ToArrayAndClear(int length)
    {
        var array = ToArray(length);
        Clear(length);
        return array;
    }

    private T[] ToArray(int length)
    {
        if (length == 0)
        {
            return Array.Empty<T>();
        }

        var array = new T[length];
        Array.Copy(_array, array, length);
        return array;
    }

    private void Clear(int length)
    {
        if (length == 0)
        {
            return;
        }

        _additions = 0;
        Array.Clear(_array, 0, length);
    }

    private static void ThrowIfLessThanTwo(int capacity, string paramName)
    {
        if (capacity < 2)
        {
            ThrowLessThanTwo(capacity, paramName);
        }
    }

    private static void ThrowLessThanTwo(int capacity, string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, capacity, "Argument must be at least two.");
    }
}
