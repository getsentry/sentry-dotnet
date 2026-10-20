using Quartz;

namespace Sentry.Quartz;

/// <summary>
/// Options for the Sentry Quartz.NET integration.
/// </summary>
public class SentryQuartzOptions
{
    /// <summary>
    /// When enabled, the in-progress check-in of each job with <see cref="SentryMonitorSlugAttribute"/> includes
    /// the schedule of the trigger that fired the job as the monitor config, so Sentry creates the monitor or updates
    /// its schedule. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only cron triggers and simple triggers that repeat forever at a whole number of minutes, hours or days are
    /// sent. Cron expressions are sent when Sentry can represent them exactly: a fixed seconds value, no year, and no
    /// <c>L</c>, <c>W</c> or <c>#</c>. Triggers with a calendar are not sent.
    /// </para>
    /// <para>
    /// When several triggers use the same monitor slug with different schedules, for example two triggers of one job,
    /// or jobs that share a slug through <see cref="SentryMonitorSlugAttribute"/>, no schedule is sent for them. Give
    /// each its own monitor slug, for example with <see cref="SentryMonitorSlugAttribute.JobDataKey"/> in each
    /// trigger's <c>JobDataMap</c>, to send it.
    /// </para>
    /// <para>
    /// Time zones that can't be converted to an IANA ID are not sent. On .NET Framework, Windows time zone IDs can't be
    /// converted, so only <c>UTC</c> and IANA IDs are sent.
    /// </para>
    /// <para>
    /// To turn this off for one job, set <see cref="SentryMonitorSlugAttribute.SendMonitorConfig"/>.
    /// </para>
    /// </remarks>
    public bool SendMonitorConfig { get; set; } = true;

    /// <summary>
    /// Configures the monitor config of a job, for example its thresholds or <see cref="SentryMonitorOptions.MaxRuntime"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called with the context of each run before its in-progress check-in, while <see cref="SendMonitorConfig"/> is
    /// enabled. A schedule set here is sent instead of the trigger's, and a time zone set here instead of the
    /// trigger's. Exceptions thrown here are logged.
    /// </para>
    /// <para>
    /// The monitor config is only sent when it has a schedule, from the trigger or set here.
    /// </para>
    /// </remarks>
    public Action<IJobExecutionContext, SentryMonitorOptions>? ConfigureMonitorOptions { get; set; }
}
