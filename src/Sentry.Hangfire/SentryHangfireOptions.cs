namespace Sentry.Hangfire;

/// <summary>
/// Options for the Sentry Hangfire integration.
/// </summary>
public class SentryHangfireOptions
{
    /// <summary>
    /// When enabled, the in-progress check-in of a recurring job includes the job's cron expression and time zone
    /// as the monitor config, so Sentry creates the monitor or keeps its schedule up to date.
    /// Schedules that Sentry can't represent are not sent. Defaults to <c>false</c>.
    /// </summary>
    public bool SendRecurringJobSchedule { get; set; }
}
