namespace Sentry;

/// <summary>
/// Session metadata.
/// </summary>
public interface ISentrySession
{
    /// <summary>
    /// Session auto-generated ID.
    /// </summary>
    public SentryId Id { get; }

    /// <summary>
    /// Session distinct ID.
    /// </summary>
    public string? DistinctId { get; }

    /// <summary>
    /// Session start timestamp.
    /// </summary>
    public DateTimeOffset StartTimestamp { get; }

    /// <summary>
    /// Release.
    /// </summary>
    public string Release { get; }

    /// <summary>
    /// Environment.
    /// </summary>
    public string? Environment { get; }

    /// <summary>
    /// IP address of the user.
    /// </summary>
    public string? IpAddress { get; }

    /// <summary>
    /// User agent.
    /// </summary>
    public string? UserAgent { get; }

    /// <summary>
    /// Reported error count.
    /// </summary>
    public int ErrorCount { get; }
}
