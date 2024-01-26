namespace Sentry;

/// <summary>
/// Checkin metadata.
/// </summary>
public interface ICheckin
{
    /// <summary>
    /// Checkin ID
    /// </summary>
    SentryId Id { get; }

    /// <summary>
    /// The distinct slug of the monitor.
    /// </summary>
    string MonitorSlug { get; }

    /// <summary>
    /// The status of the Checkin
    /// </summary>
    CheckinStatus Status { get; }
}

/// <summary>
/// The Checkin Status
/// </summary>
public enum CheckinStatus
{
    /// <summary>
    /// The Checkin is in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// The Checkin is Ok
    /// </summary>
    Ok,

    /// <summary>
    /// The Checkin errored
    /// </summary>
    Error
}

/// <summary>
/// Sentry Checkin
/// </summary>
// https://develop.sentry.dev/sdk/check-ins/
public class Checkin : ICheckin
{
    /// <inheritdoc />
    public SentryId Id { get; }

    /// <inheritdoc />
    public string MonitorSlug { get; }

    /// <inheritdoc />
    public CheckinStatus Status { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="Checkin"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="monitorSlug"></param>
    /// <param name="status"></param>
    public Checkin(SentryId id, string monitorSlug, CheckinStatus status)
    {
        Id = id;
        MonitorSlug = monitorSlug;
        Status = status;
    }
}
