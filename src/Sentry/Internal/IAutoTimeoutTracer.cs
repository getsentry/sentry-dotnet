namespace Sentry.Internal;

/// <summary>
/// Internal interface for transactions that support auto-timeout reset. Added as internal to prevent a breaking change.
/// We could make this public in the next major release (although it is really an implementation depail so internal
/// isn't that bad).
/// </summary>
internal interface IAutoTimeoutTracer
{
    /// <summary>
    /// Resets the idle timeout for auto-finishing transactions.
    /// </summary>
    public void ResetIdleTimeout();
}
