namespace Sentry.Quartz;

/// <summary>
/// Sentry Monitor Slug Attribute
/// </summary>
/// <param name="monitorSlug"></param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SentryCronMonitorSlugAttribute(string? monitorSlug = null) : Attribute
{
    /// <summary>
    /// Gets the slug associated with the Sentry monitor. The monitor slug is used to
    /// identify a specific Sentry monitor associated with a job. If no slug is explicitly
    /// provided through the <see cref="SentryCronMonitorSlugAttribute"/>, it defaults to
    /// the name of the job type.
    /// </summary>
    public string? MonitorSlug { get; } = monitorSlug;
}
