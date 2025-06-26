namespace Sentry.Internal;

internal sealed class BatchBuffer<T>
{
    private readonly T[] _array;
    private int _count;

    public BatchBuffer(int capacity)
    {
        _array = new T[capacity];
        _count = 0;
    }

    internal int Count => _count;
    internal int Capacity => _array.Length;
    internal bool IsEmpty => _count == 0 && _array.Length != 0;
    internal bool IsFull => _count == _array.Length;

    internal bool TryAdd(T item)
    {
        var count = Interlocked.Increment(ref _count);

        if (count <= _array.Length)
        {
            _array[count - 1] = item;
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
}
