namespace Sentry.Internal;

/// <summary>
/// A slim wrapper over an <see cref="System.Array"/>, intended for buffering.
/// <para>Requires a minimum capacity of 2.</para>
/// </summary>
/// <remarks>
/// Not all members are thread-safe.
/// See individual members for notes on thread safety.
/// </remarks>
internal sealed class BatchBuffer<T>
{
    private readonly T[] _array;
    private int _additions;

    /// <summary>
    /// Create a new buffer.
    /// </summary>
    /// <param name="capacity">Length of new the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="capacity"/> is less than <see langword="2"/>.</exception>
    public BatchBuffer(int capacity)
    {
        ThrowIfLessThanTwo(capacity, nameof(capacity));

        _array = new T[capacity];
        _additions = 0;
    }

    /// <summary>
    /// Maximum number of elements that can be added to the buffer.
    /// </summary>
    /// <remarks>
    /// This property is thread-safe.
    /// </remarks>
    internal int Capacity => _array.Length;

    /// <summary>
    /// Have any elements been added to the buffer?
    /// </summary>
    /// <remarks>
    /// This property is not thread-safe.
    /// </remarks>
    internal bool IsEmpty => _additions == 0;

    /// <summary>
    /// Have <see cref="Capacity"/> number of elements been added to the buffer?
    /// </summary>
    /// <remarks>
    /// This property is not thread-safe.
    /// </remarks>
    internal bool IsFull => _additions >= _array.Length;

    /// <summary>
    /// Attempt to atomically add one element to the buffer.
    /// </summary>
    /// <param name="item">Element attempted to be added atomically.</param>
    /// <param name="count">When this method returns <see langword="true"/>, is set to the Length at which the <paramref name="item"/> was added at.</param>
    /// <returns><see langword="true"/> when <paramref name="item"/> was added atomically; <see langword="false"/> when <paramref name="item"/> was not added.</returns>
    /// <remarks>
    /// This method is thread-safe.
    /// </remarks>
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

    /// <summary>
    /// Returns a new Array consisting of the elements successfully added.
    /// </summary>
    /// <returns>An Array with Length of successful additions.</returns>
    /// <remarks>
    /// This method is not thread-safe.
    /// </remarks>
    internal T[] ToArrayAndClear()
    {
        var additions = _additions;
        var length = _array.Length;
        if (additions < length)
        {
            length = additions;
        }
        return ToArrayAndClear(length);
    }

    /// <summary>
    /// Returns a new Array consisting of <paramref name="length"/> elements successfully added.
    /// </summary>
    /// <param name="length">The Length of the buffer a new Array is created from.</param>
    /// <returns>An Array with Length of <paramref name="length"/>.</returns>
    /// <remarks>
    /// This method is not thread-safe.
    /// </remarks>
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
