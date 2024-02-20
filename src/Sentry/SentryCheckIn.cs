using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// The Checkin Status
/// </summary>
public enum CheckInStatus
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
public class SentryCheckIn : ISentryJsonSerializable
{
    /// <summary>
    /// CheckIn ID
    /// </summary>
    public SentryId Id { get; }

    /// <summary>
    /// The distinct slug of the monitor.
    /// </summary>
    public string MonitorSlug { get; }

    /// <summary>
    /// The status of the Checkin
    /// </summary>
    public CheckInStatus Status { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SentryCheckIn"/>.
    /// </summary>
    /// <param name="monitorSlug"></param>
    /// <param name="status"></param>
    /// <param name="sentryId"></param>
    public SentryCheckIn(string monitorSlug, CheckInStatus status, SentryId? sentryId = null)
    {
        MonitorSlug = monitorSlug;
        Status = status;
        Id = sentryId ?? SentryId.Create();
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("check_in_id", Id, logger);
        writer.WriteString("monitor_slug", MonitorSlug);
        writer.WriteString("status", ToSnakeCase(Status));

        writer.WriteEndObject();
    }

    private static string ToSnakeCase(CheckInStatus status)
    {
        return status switch
        {
            CheckInStatus.InProgress => "in_progress",
            CheckInStatus.Ok => "ok",
            CheckInStatus.Error => "error",
            _ => throw new ArgumentException($"Unsupported CheckInStatus: '{status}'.")
        };
    }
}
