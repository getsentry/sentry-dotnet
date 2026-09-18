namespace Sentry.Infrastructure;

/// <summary>
/// Abstraction over a one-shot timer, to allow deterministic testing.
/// </summary>
internal interface ISentryTimer : IDisposable
{
    /// <summary>
    /// Starts (or restarts) the timer to fire after <paramref name="timeout"/>.
    /// </summary>
    public void Start(TimeSpan timeout);

    /// <summary>
    /// Cancels any pending fire. Has no effect if the timer is already cancelled.
    /// </summary>
    public void Cancel();
}
