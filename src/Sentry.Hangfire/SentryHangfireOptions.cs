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

    /// <summary>
    /// When enabled, every recurring job sends check-ins, not only jobs marked with
    /// <see cref="SentryMonitorSlugAttribute"/>. The monitor slug is the recurring job ID, lowercased, with every run of
    /// characters other than letters, digits and <c>_</c> replaced by <c>-</c>, and cut to 50 characters.
    /// Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Jobs marked with <see cref="SentryMonitorSlugAttribute"/> keep their monitor slug.
    /// Use <see cref="ExcludedRecurringJobs"/> to leave out some recurring jobs.
    /// </remarks>
    public bool MonitorAllRecurringJobs { get; set; }

    /// <summary>
    /// IDs of recurring jobs that <see cref="MonitorAllRecurringJobs"/> doesn't send check-ins for. IDs must match
    /// exactly.
    /// </summary>
    /// <remarks>
    /// Jobs marked with <see cref="SentryMonitorSlugAttribute"/> still send check-ins.
    /// </remarks>
    public ICollection<string> ExcludedRecurringJobs { get; set; } = new List<string>();

    internal string? GetRecurringJobSlug(string recurringJobId)
    {
        if (!MonitorAllRecurringJobs || ExcludedRecurringJobs.Contains(recurringJobId))
        {
            return null;
        }

        var slug = InvalidSlugCharacters.Replace(recurringJobId.ToLowerInvariant(), "-").Trim('-', '_');
        if (slug.Length > MaxSlugLength)
        {
            slug = slug.Substring(0, MaxSlugLength).Trim('-', '_');
        }
        return slug.Length > 0 ? slug : null;
    }

    private const int MaxSlugLength = 50;
    private static readonly Regex InvalidSlugCharacters = new("[^a-z0-9_]+", RegexOptions.Compiled);
}
