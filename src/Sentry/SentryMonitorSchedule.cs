namespace Sentry;

/// <summary>
/// The available Monitor Schedule Types
/// </summary>
internal enum SentryMonitorScheduleType
{
    /// <summary>
    /// Crontab
    /// </summary>
    Crontab,

    /// <summary>
    /// Interval
    /// </summary>
    Interval
}

/// <summary>
/// Sentry's Monitor Schedule
/// </summary>
internal class SentryMonitorSchedule
{
    /// <summary>
    /// Monitor Schedule Type
    /// </summary>
    public SentryMonitorScheduleType Type { get; set; }

    internal string? Crontab { get; set; }
    internal int? Interval { get; set; }
    internal string? Unit { get; set; }

    public SentryMonitorSchedule(
        SentryMonitorScheduleType type,
        string? crontab = null,
        int? interval = null,
        string? unit = null)
    {
        Type = type;
        Crontab = crontab;
        Interval = interval;
        Unit = unit;
    }
}
