namespace Sentry.Quartz;

/// <summary>
/// Options for <see cref="SentryJobListener"/>.
/// </summary>
public class SentryQuartzOptions
{
    /// <summary>
    /// When enabled, the in-progress check-in includes the schedule of the trigger that fired the job as the monitor
    /// config, so Sentry creates the monitor or updates its schedule. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only cron triggers and simple triggers that repeat forever at a whole number of minutes, hours or days are
    /// sent. Cron expressions are sent when Sentry can represent them exactly: a fixed seconds value, no year, and no
    /// <c>L</c>, <c>W</c> or <c>#</c>. Triggers with a calendar, and jobs with more than one trigger that share a
    /// monitor, are not sent.
    /// </para>
    /// <para>
    /// Time zones that can't be converted to an IANA ID are not sent. On .NET Framework, Windows time zone IDs can't be
    /// converted, so only <c>UTC</c> and IANA IDs are sent.
    /// </para>
    /// </remarks>
    public bool SendMonitorConfig { get; set; } = true;
}
