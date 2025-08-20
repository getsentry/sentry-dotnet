using Sentry.Threading;

namespace Sentry.Internal;

/// <summary>
/// A wrapper over an <see cref="System.Array"/>, intended for reusable buffering.
/// </summary>
/// <remarks>
/// Must be attempted to flush via <see cref="TryEnterFlushScope"/> when either the <see cref="Capacity"/> is reached,
/// or when the <see cref="_timeout"/> is exceeded.
/// Utilizes a <see cref="ScopedCountdownLock"/>, basically used as an inverse <see cref="ReaderWriterLockSlim"/>,
/// allowing multiple threads for <see cref="Add"/> or exclusive access for <see cref="FlushScope.Flush"/>.
/// </remarks>
[DebuggerDisplay("Name = {Name}, Capacity = {Capacity}, Additions = {_additions}, AddCount = {AddCount}, IsDisposed = {_disposed}")]
internal sealed class StructuredLogBatchBuffer : IDisposable
{
    private readonly SentryLog[] _array;
    private int _additions;
    private readonly ScopedCountdownLock _addLock;

    private readonly Timer _timer;
    private readonly TimeSpan _timeout;
    private readonly Action<StructuredLogBatchBuffer> _timeoutExceededAction;

    private volatile bool _disposed;

    /// <summary>
    /// Create a new buffer.
    /// </summary>
    /// <param name="capacity">Length of the new buffer.</param>
    /// <param name="timeout">When the timeout exceeds after an item has been added and the <paramref name="capacity"/> not yet been exceeded, <paramref name="timeoutExceededAction"/> is invoked.</param>
    /// <param name="timeoutExceededAction">The operation to execute when the <paramref name="timeout"/> exceeds if the buffer is neither empty nor full.</param>
    /// <param name="name">Name of the new buffer.</param>
    public StructuredLogBatchBuffer(int capacity, TimeSpan timeout, Action<StructuredLogBatchBuffer> timeoutExceededAction, string? name = null)
    {
        ThrowIfLessThanTwo(capacity, nameof(capacity));
        ThrowIfNegativeOrZero(timeout, nameof(timeout));

        _array = new SentryLog[capacity];
        _additions = 0;
        _addLock = new ScopedCountdownLock();

        _timer = new Timer(OnIntervalElapsed, this, Timeout.Infinite, Timeout.Infinite);
        _timeout = timeout;
        _timeoutExceededAction = timeoutExceededAction;

        Name = name ?? "default";
    }

    /// <summary>
    /// Name of the buffer.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Maximum number of elements that can be added to the buffer.
    /// </summary>
    internal int Capacity => _array.Length;

    /// <summary>
    /// Gets a value indicating whether this buffer is empty.
    /// </summary>
    internal bool IsEmpty => _additions == 0;

    /// <summary>
    /// Number of <see cref="Add"/> operations in progress.
    /// </summary>
    private int AddCount => _addLock.Count;

    /// <summary>
    /// Attempt to add one element to the buffer.
    /// Is thread-safe.
    /// </summary>
    /// <param name="item">Element attempted to be added.</param>
    /// <returns>An <see cref="StructuredLogBatchBufferAddStatus"/> describing the result of the thread-safe operation.</returns>
    internal StructuredLogBatchBufferAddStatus Add(SentryLog item)
    {
        if (_disposed)
        {
            return StructuredLogBatchBufferAddStatus.IgnoredIsDisposed;
        }

        using var scope = _addLock.TryEnterCounterScope();
        if (!scope.IsEntered)
        {
            return StructuredLogBatchBufferAddStatus.IgnoredIsFlushing;
        }

        var count = Interlocked.Increment(ref _additions);

        if (count == 1)
        {
            EnableTimer();
            _array[count - 1] = item;
            return StructuredLogBatchBufferAddStatus.AddedFirst;
        }

        if (count < _array.Length)
        {
            _array[count - 1] = item;
            return StructuredLogBatchBufferAddStatus.Added;
        }

        if (count == _array.Length)
        {
            DisableTimer();
            _array[count - 1] = item;
            return StructuredLogBatchBufferAddStatus.AddedLast;
        }

        Debug.Assert(count > _array.Length);
        return StructuredLogBatchBufferAddStatus.IgnoredCapacityExceeded;
    }

    /// <summary>
    /// Enters a <see cref="FlushScope"/> used to ensure that only a single flush operation is in progress.
    /// </summary>
    /// <remarks>
    /// Must be disposed to exit.
    /// </remarks>
    internal FlushScope TryEnterFlushScope()
    {
        if (_disposed)
        {
            return new FlushScope();
        }

        var scope = _addLock.TryEnterLockScope();
        if (scope.IsEntered)
        {
            return new FlushScope(this, scope);
        }

        return new FlushScope();
    }

    /// <summary>
    /// Exits the <see cref="FlushScope"/> through <see cref="FlushScope.Dispose"/>.
    /// </summary>
    private void ExitFlushScope()
    {
        Debug.Assert(_addLock.IsEngaged);
    }

    /// <summary>
    /// Callback when Timer has elapsed after first item has been added and buffer is not full yet.
    /// </summary>
    internal void OnIntervalElapsed(object? state)
    {
        if (!_disposed)
        {
            _timeoutExceededAction(this);
        }
    }

    /// <summary>
    /// Returns a new Array consisting of the elements successfully added.
    /// </summary>
    /// <returns>An Array with Length of successful additions.</returns>
    private SentryLog[] ToArrayAndClear()
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
    private SentryLog[] ToArrayAndClear(int length)
    {
        Debug.Assert(_addLock.IsSet);

        var array = ToArray(length);
        Clear(length);
        return array;
    }

    private SentryLog[] ToArray(int length)
    {
        if (length == 0)
        {
            return Array.Empty<SentryLog>();
        }

        var array = new SentryLog[length];
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

    private void EnableTimer()
    {
        _ = _timer.Change(_timeout, Timeout.InfiniteTimeSpan);
    }

    private void DisableTimer()
    {
        _ = _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Dispose();
        _addLock.Dispose();
        _disposed = true;
    }

    private static void ThrowIfLessThanTwo(int value, string paramName)
    {
        if (value < 2)
        {
            ThrowLessThanTwo(value, paramName);
        }

        static void ThrowLessThanTwo(int value, string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Argument must be at least two.");
        }
    }

    private static void ThrowIfNegativeOrZero(TimeSpan value, string paramName)
    {
        if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        {
            ThrowNegativeOrZero(value, paramName);
        }

        static void ThrowNegativeOrZero(TimeSpan value, string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Argument must be a non-negative and non-zero value.");
        }
    }

    /// <summary>
    /// A scope than ensures only a single <see cref="Flush"/> operation is in progress,
    /// and blocks the calling thread until all <see cref="Add"/> operations have finished.
    /// When <see cref="IsEntered"/> is <see langword="true"/>, no more <see cref="Add"/> can be started,
    /// which will then return <see cref="StructuredLogBatchBufferAddStatus.IgnoredIsFlushing"/> immediately.
    /// </summary>
    /// <remarks>
    /// Only <see cref="Flush"/> when scope <see cref="IsEntered"/>.
    /// </remarks>
    internal ref struct FlushScope : IDisposable
    {
        private StructuredLogBatchBuffer? _lockObj;
        private ScopedCountdownLock.LockScope _scope;

        internal FlushScope(StructuredLogBatchBuffer lockObj, ScopedCountdownLock.LockScope scope)
        {
            Debug.Assert(scope.IsEntered);
            _lockObj = lockObj;
            _scope = scope;
        }

        internal bool IsEntered => _scope.IsEntered;

        internal SentryLog[] Flush()
        {
            var lockObj = _lockObj;
            if (lockObj is not null)
            {
                _scope.Wait();

                var array = lockObj.ToArrayAndClear();
                return array;
            }

            throw new ObjectDisposedException(nameof(FlushScope));
        }

        public void Dispose()
        {
            var lockObj = _lockObj;
            if (lockObj is not null)
            {
                _lockObj = null;
                lockObj.ExitFlushScope();
            }

            _scope.Dispose();
        }
    }
}

internal enum StructuredLogBatchBufferAddStatus : byte
{
    AddedFirst,
    Added,
    AddedLast,
    IgnoredCapacityExceeded,
    IgnoredIsFlushing,
    IgnoredIsDisposed,
}
