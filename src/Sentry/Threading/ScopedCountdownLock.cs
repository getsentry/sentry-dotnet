namespace Sentry.Threading;

/// <summary>
/// A synchronization primitive that tracks the amount of <see cref="CounterScope"/>s held,
/// and is signaled when the count reaches zero while a <see cref="LockScope"/> is held.
/// </summary>
/// <remarks>
/// Similar to <see cref="CountdownEvent"/>,
/// but allows to increment the current count after the count has reached zero without resetting to the initial count before a <see cref="LockScope"/> is entered.
/// Has a similar API shape to System.Threading.Lock.
/// </remarks>
[DebuggerDisplay("IsSet = {IsSet}, Count = {Count}, IsEngaged = {IsEngaged}")]
internal sealed class ScopedCountdownLock : IDisposable
{
    private readonly CountdownEvent _event;
    private volatile int _isEngaged;

    internal ScopedCountdownLock()
    {
        _event = new CountdownEvent(1);
        _isEngaged = 0;
    }

    /// <summary>
    /// <see langword="true"/> if the event is set/signaled; otherwise, <see langword="false"/>.
    /// When <see langword="true"/>, the active <see cref="LockScope"/> can <see cref="LockScope.Wait"/> until the <see cref="Count"/> reaches <see langword="0"/>.
    /// </summary>
    internal bool IsSet => _event.IsSet;

    /// <summary>
    /// Gets the number of remaining <see cref="CounterScope"/> required to exit in order to set/signal the event while a <see cref="LockScope"/> is active.
    /// When <see langword="0"/> and while a <see cref="LockScope"/> is active, no more <see cref="CounterScope"/> can be entered.
    /// </summary>
    internal int Count => _isEngaged == 1 ? _event.CurrentCount : _event.CurrentCount - 1;

    /// <summary>
    /// Returns <see langword="true"/> when a <see cref="LockScope"/> is active and the event can be set/signaled by <see cref="Count"/> reaching <see langword="0"/>.
    /// Returns <see langword="false"/> when the <see cref="Count"/> can only reach the initial count of <see langword="1"/> when no <see cref="CounterScope"/> is active any longer.
    /// </summary>
    internal bool IsEngaged => _isEngaged == 1;

    /// <summary>
    /// No <see cref="CounterScope"/> will be entered when the <see cref="Count"/> has reached <see langword="0"/> while the lock is engaged via an active <see cref="LockScope"/>.
    /// Check via <see cref="CounterScope.IsEntered"/> whether the underlying <see cref="CountdownEvent"/> has not been set/signaled yet.
    /// To signal the underlying <see cref="CountdownEvent"/>, ensure <see cref="CounterScope.Dispose"/> is called.
    /// </summary>
    /// <remarks>
    /// Must be disposed to exit.
    /// </remarks>
    internal CounterScope TryEnterCounterScope()
    {
        if (_event.TryAddCount(1))
        {
            return new CounterScope(this);
        }

        return new CounterScope();
    }

    private void ExitCounterScope()
    {
        _ = _event.Signal();
    }

    /// <summary>
    /// When successful, the lock <see cref="IsEngaged"/>, <see cref="Count"/> can reach <see langword="0"/> when no <see cref="CounterScope"/> is active, and the event can be set/signaled.
    /// Check via <see cref="LockScope.IsEntered"/> whether the Lock <see cref="IsEngaged"/>.
    /// Use <see cref="LockScope.Wait"/> to block until every active <see cref="CounterScope"/> has exited before performing the locked operation.
    /// After the locked operation has completed, disengage the Lock via <see cref="LockScope.Dispose"/>.
    /// </summary>
    /// <remarks>
    /// Must be disposed to exit.
    /// </remarks>
    internal LockScope TryEnterLockScope()
    {
        if (Interlocked.CompareExchange(ref _isEngaged, 1, 0) == 0)
        {
            _ = _event.Signal(); // decrement the initial count of 1, so that the event can be set with the count reaching 0 when all 'CounterScope's have exited
            return new LockScope(this);
        }

        return new LockScope();
    }

    private void ExitLockScope()
    {
        _event.Reset(); // reset the signaled event to the initial count of 1, so that new `CounterScope`s can be entered again

        if (Interlocked.CompareExchange(ref _isEngaged, 0, 1) != 1)
        {
            Debug.Fail("The Lock should have not been disengaged without being engaged first.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _event.Dispose();
    }

    internal ref struct CounterScope : IDisposable
    {
        private ScopedCountdownLock? _lockObj;

        internal CounterScope(ScopedCountdownLock lockObj)
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
                lockObj.ExitCounterScope();
            }
        }
    }

    internal ref struct LockScope : IDisposable
    {
        private ScopedCountdownLock? _lockObj;

        internal LockScope(ScopedCountdownLock lockObj)
        {
            _lockObj = lockObj;
        }

        internal bool IsEntered => _lockObj is not null;

        /// <summary>
        /// Blocks the current thread until the current <see cref="Count"/> reaches <see langword="0"/> and the event is set/signaled.
        /// The caller will return immediately if the event is currently in a set/signaled state.
        /// </summary>
        internal void Wait()
        {
            var lockObj = _lockObj;
            lockObj?._event.Wait(Timeout.Infinite, CancellationToken.None);
        }

        public void Dispose()
        {
            var lockObj = _lockObj;
            if (lockObj is not null)
            {
                _lockObj = null;
                lockObj.ExitLockScope();
            }
        }
    }
}
