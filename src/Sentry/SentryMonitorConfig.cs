using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

internal enum SentryMonitorScheduleType
{
    Crontab,
    Interval
}

/// <summary>
/// Sentry's config for monitors
/// </summary>
public class SentryMonitorConfig : ISentryJsonSerializable
{
    private SentryMonitorScheduleType Type { get; }
    private string? Crontab { get; }
    private int? Interval { get; }
    private MeasurementUnit.Duration? Unit { get; }

    /// <summary>
    /// The allowed margin of minutes after the expected check-in time that the monitor will not be considered missed for.
    /// </summary>
    public TimeSpan? CheckInMargin { get; set; }

    /// <summary>
    /// The allowed duration in minutes that the monitor may be in_progress for before being considered failed due to timeout.
    /// </summary>
    public TimeSpan? MaxRuntime { get; set; }

    /// <summary>
    /// The number of consecutive failed check-ins it takes before an issue is created.
    /// </summary>
    public int? FailureIssueThreshold { get; set; }

    /// <summary>
    /// The number of consecutive OK check-ins it takes before an issue is resolved.
    /// </summary>
    public int RecoveryThreshold { get; set; }

    /// <summary>
    /// The timezone which the monitor's execution schedule is in.
    /// </summary>
    public TimeZoneInfo? Timezone { get; set; }

    /// <summary>
    /// An actor identifier string.
    /// </summary>
    public string? Owner { get; set; }

    internal SentryMonitorConfig(
        SentryMonitorScheduleType type,
        string? crontab = null,
        int? interval = null,
        MeasurementUnit.Duration? unit = null)
    {
        Type = type;
        Crontab = crontab;
        Interval = interval;
        Unit = unit;
    }

    /// <summary>
    /// Creates a new Monitor Config based on a crontab
    /// </summary>
    /// <param name="crontab"></param>
    /// <returns></returns>
    public static SentryMonitorConfig CreateCronMonitorConfig(string crontab)
        => new(SentryMonitorScheduleType.Crontab, crontab: crontab);

    /// <summary>
    /// Creates a new Monitor Config based on an interval
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="unit"></param>
    /// <returns></returns>
    public static SentryMonitorConfig CreateIntervalMonitorConfig(int interval, MeasurementUnit.Duration unit)
        => new(SentryMonitorScheduleType.Interval, interval: interval, unit: unit);

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject("monitor_config");

        writer.WriteStartObject("schedule");

        writer.WriteString("type", TypeToString(Type));
        switch (Type)
        {
            case SentryMonitorScheduleType.Crontab:
                writer.WriteStringIfNotWhiteSpace("value", Crontab);
                break;
            case SentryMonitorScheduleType.Interval:
                writer.WriteNumberIfNotNull("value", Interval);
                writer.WriteStringIfNotWhiteSpace("unit", Unit.ToString()?.ToLower());
                break;
        }

        writer.WriteEndObject();

        writer.WriteNumberIfNotNull("checkin_margin", CheckInMargin?.TotalMinutes);
        writer.WriteNumberIfNotNull("max_runtime", MaxRuntime?.TotalMinutes);
        writer.WriteNumberIfNotNull("failure_issue_threshold", FailureIssueThreshold);
        writer.WriteNumberIfNotNull("recovery_threshold", RecoveryThreshold);
        writer.WriteStringIfNotWhiteSpace("timezone", Timezone?.Id);
        writer.WriteStringIfNotWhiteSpace("owner", Owner);

        writer.WriteEndObject();
    }

    private static string TypeToString(SentryMonitorScheduleType type)
    {
        return type switch
        {
            SentryMonitorScheduleType.Crontab => "crontab",
            SentryMonitorScheduleType.Interval => "interval",
            _ => throw new ArgumentException($"Unsupported Monitor Schedule Type: '{type}'.")
        };
    }
}
