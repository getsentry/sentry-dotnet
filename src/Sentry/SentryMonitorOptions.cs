using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

internal enum SentryMonitorScheduleType
{
    None,
    Crontab,
    Interval
}

/// <summary>
/// Sentry's intervals for monitors
/// </summary>
public enum SentryMonitorInterval
{
    /// <summary>
    /// Year
    /// </summary>
    Year,

    /// <summary>
    /// Month
    /// </summary>
    Month,

    /// <summary>
    /// Week
    /// </summary>
    Week,

    /// <summary>
    /// Day
    /// </summary>
    Day,

    /// <summary>
    /// Hour
    /// </summary>
    Hour,

    /// <summary>
    /// Minute
    /// </summary>
    Minute
}

/// <summary>
/// Sentry's options for monitors
/// </summary>
public partial class SentryMonitorOptions : ISentryJsonSerializable
{
    private SentryMonitorScheduleType _type = SentryMonitorScheduleType.None;
    private string? _crontab;
    private int? _interval;
    private SentryMonitorInterval? _unit;

    // Breakdown of the validation
    // ^(\*|([0-5]?\d))                 Minute  0 - 59
    // (\s+)(\*|([01]?\d|2[0-3]))       Hour    0 - 23
    // (\s+)(\*|([1-9]|[12]\d|3[01]))   Day     1 - 31
    // (\s+)(\*|([1-9]|1[0-2]))         Month   1 - 12
    // (\s+)(\*|([0-7]))                Weekday 0 - 7
    // $                                End of string
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^(\*|([0-5]?\d))(\s+)(\*|([01]?\d|2[0-3]))(\s+)(\*|([1-9]|[12]\d|3[01]))(\s+)(\*|([1-9]|1[0-2]))(\s+)(\*|([0-7]))$", RegexOptions.IgnoreCase)]
    private static partial Regex CrontabValidation();
#else
    private static Regex? CrontabValidationInstance;

    private static Regex CrontabValidation()
    {
        return CrontabValidationInstance ??= new Regex(
            @"^(\*|([0-5]?\d))(\s+)(\*|([01]?\d|2[0-3]))(\s+)(\*|([1-9]|[12]\d|3[01]))(\s+)(\*|([1-9]|1[0-2]))(\s+)(\*|([0-7]))$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
#endif

    /// <summary>
    /// Set Interval
    /// </summary>
    /// <param name="cronTab"></param>
    public void Interval(string cronTab)
    {
        if (_type is not SentryMonitorScheduleType.None)
        {
            throw new ArgumentException("You tried to set the interval twice. The Check-Ins interval is supposed to be set only once.");
        }

        if (!CrontabValidation().IsMatch(cronTab))
        {
            throw new ArgumentException("The provided crontab does not match the expected format of '* * * * *' " +
                                        "translating to 'minute', 'hour', 'day of the month', 'month', and 'day of the week'.");
        }

        _type = SentryMonitorScheduleType.Crontab;
        _crontab = cronTab;
    }

    /// <summary>
    /// Set Interval
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="unit"></param>
    public void Interval(int interval, SentryMonitorInterval unit)
    {
        if (_type is not SentryMonitorScheduleType.None)
        {
            throw new ArgumentException("You tried to set the interval twice. The Check-Ins interval is supposed to be set only once.");
        }

        _type = SentryMonitorScheduleType.Interval;
        _interval = interval;
        _unit = unit;
    }

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
    /// A tz database string representing the timezone which the monitor's execution schedule is in (i.e. "America/Los_Angeles").
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// An actor identifier string. This looks like 'user:john@example.com team:a-sentry-team'. IDs can also be used but will result in a poor DX.
    /// </summary>
    public string? Owner { get; set; }

    internal SentryMonitorOptions() { }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        Debug.Assert(_type != SentryMonitorScheduleType.None, "The Monitor Options do not contain a valid interval." +
                                                              "Please update your monitor options by setting the Interval.");

        writer.WriteStartObject("monitor_config");
        writer.WriteStartObject("schedule");

        writer.WriteString("type", TypeToString(_type));
        switch (_type)
        {
            case SentryMonitorScheduleType.Crontab:
                Debug.Assert(string.IsNullOrEmpty(_crontab), "The provided 'crontab' cannot be an empty string.");
                writer.WriteStringIfNotWhiteSpace("value", _crontab);
                break;
            case SentryMonitorScheduleType.Interval:
                Debug.Assert(_interval != null, "The provided 'interval' cannot be null.");
                writer.WriteNumberIfNotNull("value", _interval);
                Debug.Assert(_unit != null, "The provided 'unit' cannot be null.");
                writer.WriteStringIfNotWhiteSpace("unit", _unit.ToString()!.ToLower());
                break;
            default:
                logger?.LogError("Invalid MonitorScheduleType: '{0}'", _type.ToString());
                break;
        }

        writer.WriteEndObject();

        writer.WriteNumberIfNotNull("checkin_margin", CheckInMargin?.TotalMinutes);
        writer.WriteNumberIfNotNull("max_runtime", MaxRuntime?.TotalMinutes);
        writer.WriteNumberIfNotNull("failure_issue_threshold", FailureIssueThreshold);
        writer.WriteNumberIfNotNull("recovery_threshold", RecoveryThreshold);
        writer.WriteStringIfNotWhiteSpace("timezone", Timezone);
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
