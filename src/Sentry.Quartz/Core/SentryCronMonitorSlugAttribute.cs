namespace Sentry.Quartz;

/// <summary>
/// Sends a Sentry check-in each time a job of this type runs.
/// </summary>
/// <remarks>
/// <para>
/// Jobs without this attribute are not monitored.
/// </para>
/// <para>
/// When no monitor slug is given, the slug is the job's key, lowercased with every run of other characters replaced
/// by a hyphen. Jobs in the default group use only the job name: <c>reports.DailyEmail</c> becomes
/// <c>reports-dailyemail</c>, and <c>DEFAULT.Cleanup</c> becomes <c>cleanup</c>. Set <see cref="JobDataKey"/> in the
/// job's or trigger's <c>JobDataMap</c> to use a different slug for that job or trigger.
/// </para>
/// <para>
/// Quartz names a job built without an identity with a new GUID each time, so such a job uses the name of its class
/// instead, and a warning is logged. Give the job an identity or a monitor slug to keep its monitor stable.
/// </para>
/// </remarks>
/// <param name="monitorSlug">The monitor slug. Defaults to the slug of the job's key.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SentryCronMonitorSlugAttribute(string? monitorSlug = null) : Attribute
{
    /// <summary>
    /// The <c>JobDataMap</c> key whose value overrides the monitor slug.
    /// </summary>
    public const string JobDataKey = "SentryMonitorSlug";

    /// <summary>
    /// The monitor slug, or <c>null</c> to use the slug of the job's key.
    /// </summary>
    public string? MonitorSlug { get; } = monitorSlug;

    /// <summary>
    /// When enabled, the in-progress check-in includes the schedule of the trigger that fired the job as the monitor
    /// config, so Sentry creates the monitor or updates its schedule. Defaults to <c>true</c>.
    /// </summary>
    /// <seealso cref="SentryQuartzOptions.SendMonitorConfig"/>
    public bool SendMonitorConfig { get; set; } = true;
}
