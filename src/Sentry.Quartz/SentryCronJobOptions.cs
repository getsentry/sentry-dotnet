namespace Sentry.Quartz;

/// <summary>
/// Represents configuration options for Sentry integration with Quartz Cron Jobs.
/// </summary>
public class SentryCronJobOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable or disable the automatic upsert of a CronMonitor when a CronTrigger is linked with the job execution context.
    /// </summary>
    /// <remarks>
    /// If set to <c>true</c>, enables the creation or update of a CronMonitor with relevant details, such as Cron expression and time zone,
    /// during a job execution that is configured with a CronTrigger. This ensures that the monitor reflects accurate scheduling metadata in Sentry.
    /// </remarks>
    public bool EnableUpsertCronMonitor { get; set; } = true;
}
