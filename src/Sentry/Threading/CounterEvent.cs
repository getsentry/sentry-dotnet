namespace Sentry.Threading;

/// <summary>
/// A synchronization primitive that tracks the amount of <see cref="Scope"/>s held.
/// </summary>
[DebuggerDisplay("Count = {Count}, IsSet = {IsSet}")]
internal sealed class CounterEvent : IDisposable
{
    private readonly ManualResetEventSlim _event;
    private int _count;

    internal CounterEvent()
    {
        _event = new ManualResetEventSlim(true);
        _count = 0;
    }

    /// <summary>
    /// <see langword="true"/> if the event is set/signaled; otherwise, <see langword="false"/>.
    /// </summary>
    /// <value>When <see langword="false"/>, <see cref="Wait()"/> blocks the calling thread until <see cref="Count"/> reaches <see langword="0"/>.</value>
    public bool IsSet => _event.IsSet;

    /// <summary>
    /// Gets the number of remaining <see cref="Scope"/>s required to exit to set/signal the event.
    /// </summary>
    /// <value>When <see langword="0"/>, the state of the event is set/signaled, which allows the thread <see cref="Wait"/>ing on the event to proceed.</value>
    internal int Count => _count;

    /// <summary>
    /// Enter a <see cref="Scope"/>.
    /// Sets the state of the event to non-signaled, which causes <see cref="Wait"/>ing threads to block.
    /// When all <see cref="Scope"/>s have exited, the event is set/signaled.
    /// </summary>
    /// <returns>A new <see cref="Scope"/>, that must be exited via <see cref="Scope.Dispose"/>.</returns>
    internal Scope EnterScope()
    {
        var count = Interlocked.Increment(ref _count);
        Debug.Assert(count > 0);

        if (count == 1)
        {
            _event.Reset();
        }

        return new Scope(this);
    }

    private void ExitScope()
    {
        var count = Interlocked.Decrement(ref _count);
        Debug.Assert(count >= 0);

        if (count == 0)
        {
            _event.Set();
        }
    }

    /// <summary>
    /// Blocks the current thread until the current <see cref="Count"/> reaches <see langword="0"/> and the event is set/signaled.
    /// </summary>
    /// <remarks>
    /// The caller of this method blocks until <see cref="Count"/> reaches <see langword="0"/>.
    /// The caller will return immediately if the event is currently in a set/signaled state.
    /// </remarks>
    internal void Wait()
    {
        _event.Wait();
    }

    public void Dispose()
    {
        _event.Dispose();
    }

    internal ref struct Scope : IDisposable
    {
        private CounterEvent? _event;

        internal Scope(CounterEvent @event)
        {
            _event = @event;
        }

        public void Dispose()
        {
            var @event = _event;
            if (@event is not null)
            {
                _event = null;
                @event.ExitScope();
            }
        }
    }
}
