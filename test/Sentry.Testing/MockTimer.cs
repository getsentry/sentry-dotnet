using Sentry.Infrastructure;

namespace Sentry.Testing;

/// <summary>
/// A deterministic <see cref="ISentryTimer"/> for use in tests. Call <see cref="Fire"/> to
/// simulate the timeout elapsing without any real waiting.
/// </summary>
public class MockTimer : ISentryTimer
{
    private Action _callback;
    private bool _disposed;

    /// <summary>Number of times <see cref="Start"/> has been called.</summary>
    public int StartCount { get; private set; }

    /// <summary>Whether the timer is currently cancelled (not ticking).</summary>
    public bool IsCancelled { get; private set; } = true;

    /// <summary>The most recent timeout passed to <see cref="Start"/>.</summary>
    public TimeSpan? LastTimeout { get; private set; }

    public MockTimer(Action callback)
    {
        _callback = callback;
    }

    /// <inheritdoc />
    public void Start(TimeSpan timeout)
    {
        LastTimeout = timeout;
        StartCount++;
        IsCancelled = false;
    }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }

    /// <summary>
    /// Manually triggers the timer callback, simulating the idle timeout elapsing.
    /// </summary>
    public void Fire()
    {
        if (!_disposed)
        {
            _callback.Invoke();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }
}
