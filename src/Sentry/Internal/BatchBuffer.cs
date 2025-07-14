using Sentry.Threading;

namespace Sentry.Internal;

/// <summary>
/// A slim wrapper over an <see cref="System.Array"/>, intended for buffering.
/// <para>Requires a minimum capacity of 2.</para>
/// </summary>
/// <remarks>
/// Not all members are thread-safe.
/// See individual members for notes on thread safety.
/// </remarks>
[DebuggerDisplay("Name = {Name}, Capacity = {Capacity}, IsEmpty = {IsEmpty}, IsFull = {IsFull}, AddCount = {AddCount}")]
internal sealed class BatchBuffer<T> : IDisposable
{
    private readonly T[] _array;
    private int _additions;
    private readonly CounterEvent _addCounter;
    private readonly NonReentrantLock _addLock;

    /// <summary>
    /// Create a new buffer.
    /// </summary>
    /// <param name="capacity">Length of the new buffer.</param>
    /// <param name="name">Name of the new buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="capacity"/> is less than <see langword="2"/>.</exception>
    public BatchBuffer(int capacity, string? name = null)
    {
        ThrowIfLessThanTwo(capacity, nameof(capacity));
        Name = name ?? "default";

        _array = new T[capacity];
        _additions = 0;
        _addCounter = new CounterEvent();
        _addLock = new NonReentrantLock();
    }

    /// <summary>
    /// Name of the buffer.
    /// </summary>
    /// <remarks>
    /// This property is thread-safe.
    /// </remarks>
    internal string Name { get; }

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
    /// Number of <see cref="TryAdd"/> operations in progress.
    /// </summary>
    /// <remarks>
    /// This property is used for debugging only.
    /// </remarks>
    private int AddCount => _addCounter.Count;

    /// <summary>
    /// Enters a <see cref="FlushScope"/> used to ensure that only a single flush operation is in progress.
    /// </summary>
    /// <returns>A <see cref="FlushScope"/> that must be disposed to exit.</returns>
    /// <remarks>
    /// This method is thread-safe.
    /// </remarks>
    internal FlushScope TryEnterFlushScope(out bool lockTaken)
    {
        if (_addLock.TryEnter())
        {
            lockTaken = true;
            return new FlushScope(this);
        }

        lockTaken = false;
        return new FlushScope();
    }

    /// <summary>
    /// Exits the <see cref="FlushScope"/> through <see cref="FlushScope.Dispose"/>.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe.
    /// </remarks>
    private void ExitFlushScope()
    {
        _addLock.Exit();
    }

    /// <summary>
    /// Blocks the current thread until all <see cref="TryAdd"/> operations have completed.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe.
    /// </remarks>
    internal void WaitAddCompleted()
    {
        _addCounter.Wait();
    }

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
        if (_addLock.IsEntered)
        {
            count = 0;
            return false;
        }

        using var scope = _addCounter.EnterScope();

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
        Debug.Assert(_addCounter.IsSet);
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

    /// <inheritdoc />
    public void Dispose()
    {
        _addCounter.Dispose();
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

    internal ref struct FlushScope : IDisposable
    {
        private BatchBuffer<T>? _lockObj;

        internal FlushScope(BatchBuffer<T> lockObj)
        {
            _lockObj = lockObj;
        }

        internal bool IsEntered => _lockObj is not null;

        public void Dispose()
        {
            var lockObj = _lockObj;
            if (lockObj is not null)
            {
                _lockObj = null;
                lockObj.ExitFlushScope();
            }
        }
    }
}
