namespace Sentry.Infrastructure;

/// <summary>
/// Implementation of <see cref="ISystemClock"/> to help testability.
/// </summary>
/// <seealso cref="ISystemClock" />
public sealed class SystemClock : ISystemClock
{
    /// <summary>
    /// Constructs a SystemClock instance.
    /// </summary>
    /// <remarks>
    /// This constructor should have been private originally.  It will be removed in a future major version.
    /// </remarks>
    private SystemClock() { }

    /// <summary>
    /// System clock singleton.
    /// </summary>
    public static readonly SystemClock Clock = new();

    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    /// <remarks>
    /// Used for testability, calls: DateTimeOffset.UtcNow
    /// </remarks>
    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
