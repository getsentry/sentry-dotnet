namespace Sentry;

/// <summary>
/// Session metadata.
/// </summary>
public interface ISentrySession
{
    /// <summary>
    /// Session auto-generated ID.
    /// </summary>
    SentryId Id { get; }

    /// <summary>
    /// Session distinct ID.
    /// </summary>
    string? DistinctId { get; }

    /// <summary>
    /// Session start timestamp.
    /// </summary>
    DateTimeOffset StartTimestamp { get; }

    /// <summary>
    /// Release.
    /// </summary>
    string Release { get; }

    /// <summary>
    /// Environment.
    /// </summary>
    string? Environment { get; }

    /// <summary>
    /// IP address of the user.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// User agent.
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Reported error count.
    /// </summary>
    int ErrorCount { get; }
}
