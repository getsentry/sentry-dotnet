namespace Sentry.Hangfire;

/// <summary>
/// Options for the Sentry Hangfire integration.
/// </summary>
public class SentryHangfireOptions
{
    /// <summary>
    /// When enabled, the in-progress check-in of a recurring job includes the job's cron expression and time zone
    /// as the monitor config, so Sentry creates the monitor or updates its schedule. Defaults to <c>true</c>.
    /// Set to <c>false</c> to manage the monitor's schedule in Sentry instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The monitor slug still comes from <see cref="SentryMonitorSlugAttribute"/>. When several recurring jobs use the
    /// same monitor slug with different cron expressions or time zones, for example one recurring job per region that
    /// runs the same method, no monitor config is sent for them. Give each its own monitor slug to send it.
    /// </para>
    /// <para>
    /// Schedules that Sentry can't represent are not sent. This includes crons that use seconds other than a fixed
    /// value, and time zones that can't be converted to an IANA ID. On .NET Framework, Windows time zone IDs can't be
    /// converted, so only <c>UTC</c> and IANA IDs are sent.
    /// </para>
    /// </remarks>
    public bool SendMonitorConfig { get; set; } = true;
}
