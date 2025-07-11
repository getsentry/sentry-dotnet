namespace Sentry.Threading;

[DebuggerDisplay("IsEntered = {IsEntered}")]
internal sealed class NonReentrantLock
{
    private int _state;

    internal NonReentrantLock()
    {
        _state = 0;
    }

    internal bool IsEntered => _state == 1;

    internal bool TryEnter()
    {
        return Interlocked.CompareExchange(ref _state, 1, 0) == 0;
    }

    internal void Exit()
    {
        if (Interlocked.Exchange(ref _state, 0) != 1)
        {
            Debug.Fail("Do not Exit the lock scope when it has not been Entered.");
        }
    }
}
