using Quartz;

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
    /// during a job execution configured with a CronTrigger. This ensures that the monitor reflects accurate scheduling metadata in Sentry.
    /// </remarks>
    public bool EnableUpsertCronMonitor { get; set; } = true;

    /// <summary>
    /// Gets or sets a callback that is invoked to configure the <see cref="SentryMonitorOptions"/> used when
    /// upserting a CronMonitor for a job execution.
    /// </summary>
    /// <remarks>
    /// This callback is only invoked when <see cref="EnableUpsertCronMonitor"/> is <c>true</c>, and is invoked
    /// after the automatic configuration derived from the job's <see cref="ICronTrigger"/> (such as the schedule
    /// and time zone) is applied, so any values set here take precedence over those automatically derived values.
    /// Use this to customize properties of the monitor, such as <see cref="SentryMonitorOptions.MaxRuntime"/> or
    /// <see cref="SentryMonitorOptions.FailureIssueThreshold"/>, or to override the automatically derived schedule
    /// and time zone, based on the <see cref="IJobDetail"/> being executed.
    /// </remarks>
    public Action<IJobDetail, SentryMonitorOptions>? ConfigureSentryMonitorOptions { get; set; }
}
