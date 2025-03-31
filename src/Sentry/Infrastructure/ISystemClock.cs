namespace Sentry.Infrastructure;

/// <summary>
/// An abstraction to the system clock.
/// </summary>
/// <remarks>
/// Agree to disagree with closing this: https://github.com/aspnet/Common/issues/151
/// </remarks>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    public DateTimeOffset GetUtcNow();
}
