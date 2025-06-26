namespace Sentry.Internal;

/// <summary>
/// A slim wrapper over an <see cref="System.Array"/>,
/// intended for buffering.
/// </summary>
internal sealed class BatchBuffer<T>
{
    private readonly T[] _array;
    private int _count;

    public BatchBuffer(int capacity)
    {
        ThrowIfNegativeOrZero(capacity, nameof(capacity));

        _array = new T[capacity];
        _count = 0;
    }

    internal int Count => _count;
    internal int Capacity => _array.Length;
    internal bool IsEmpty => _count == 0 && _array.Length != 0;
    internal bool IsFull => _count == _array.Length;

    internal bool TryAdd(T item)
    {
        if (_count < _array.Length)
        {
            _array[_count] = item;
            _count++;
            return true;
        }

        return false;
    }

    internal T[] ToArray()
    {
        if (_count == 0)
        {
            return Array.Empty<T>();
        }

        var array = new T[_count];
        Array.Copy(_array, array, _count);
        return array;
    }

    internal void Clear()
    {
        if (_count == 0)
        {
            return;
        }

        var count = _count;
        _count = 0;
        Array.Clear(_array, 0, count);
    }

    internal T[] ToArrayAndClear()
    {
        var array = ToArray();
        Clear();
        return array;
    }

    private static void ThrowIfNegativeOrZero(int capacity, string paramName)
    {
        if (capacity <= 0)
        {
            ThrowNegativeOrZero(capacity, paramName);
        }
    }

    private static void ThrowNegativeOrZero(int capacity, string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, capacity, "Argument must neither be negative nor zero.");
    }
}
