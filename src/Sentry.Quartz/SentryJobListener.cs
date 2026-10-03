using Quartz;
using Sentry.Extensibility;

namespace Sentry.Quartz;

/// <summary>
/// A Quartz.NET job listener that captures a Sentry check-in each time a job runs.
/// </summary>
/// <remarks>
/// <para>
/// The monitor slug is the job's key, lowercased with every run of other characters replaced by a hyphen. Jobs in the
/// default group use only the job name: <c>reports.DailyEmail</c> becomes <c>reports-dailyemail</c>, and
/// <c>DEFAULT.Cleanup</c> becomes <c>cleanup</c>. Set <see cref="MonitorSlugKey"/> in the job's or trigger's
/// <see cref="JobDataMap"/> to use a different slug.
/// </para>
/// <para>
/// To monitor only some jobs, pass Quartz matchers when adding the listener.
/// </para>
/// </remarks>
public class SentryJobListener : IJobListener
{
    /// <summary>
    /// The <see cref="JobDataMap"/> key whose value overrides the monitor slug.
    /// </summary>
    public const string MonitorSlugKey = "SentryMonitorSlug";

    private const int MaxSlugLength = 50;

    private static readonly object CheckInIdKey = new();

    private readonly IHub _hub;
    private readonly SentryQuartzOptions _options;
    private readonly IDiagnosticLogger? _logger;

    /// <summary>
    /// Creates a listener with the default options.
    /// </summary>
    public SentryJobListener() : this(new SentryQuartzOptions())
    {
    }

    /// <summary>
    /// Creates a listener with the given options.
    /// </summary>
    /// <param name="options">The options.</param>
    public SentryJobListener(SentryQuartzOptions options) : this(options, HubAdapter.Instance)
    {
    }

    internal SentryJobListener(
        SentryQuartzOptions options,
        IHub hub,
        IDiagnosticLogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hub = hub;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Sentry";

    private IDiagnosticLogger? Logger => _logger ?? _hub.GetSentryOptions()?.DiagnosticLogger;

    /// <inheritdoc />
    public async Task JobToBeExecuted(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var monitorSlug = GetMonitorSlug(context);
        if (monitorSlug is null)
        {
            Logger?.LogDebug("Skipping the check-in for job '{0}'. Its key has no characters a monitor slug can use. " +
                             "Set '{1}' in the job's data map to choose a slug.", context.JobDetail.Key, MonitorSlugKey);
            return;
        }

        var configureMonitorOptions = _options.SendMonitorConfig
            ? await GetMonitorConfigAsync(context, cancellationToken).ConfigureAwait(false)
            : null;

        var checkInId = _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress, configureMonitorOptions: configureMonitorOptions);
        context.Put(CheckInIdKey, checkInId);
    }

    /// <inheritdoc />
    public Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        if (context.Get(CheckInIdKey) is SentryId checkInId && GetMonitorSlug(context) is { } monitorSlug)
        {
            var status = jobException is null ? CheckInStatus.Ok : CheckInStatus.Error;
            _hub.CaptureCheckIn(monitorSlug, status, checkInId, context.JobRunTime);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task JobExecutionVetoed(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private async Task<Action<SentryMonitorOptions>?> GetMonitorConfigAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var trigger = context.Trigger;
        try
        {
            var configureMonitorOptions = TriggerSchedule.ToMonitorConfig(trigger);
            if (configureMonitorOptions is null)
            {
                Logger?.LogDebug("Not sending the schedule of trigger '{0}'. Sentry can't represent it.", trigger.Key);
                return null;
            }

            if (!trigger.JobDataMap.ContainsKey(MonitorSlugKey))
            {
                var triggers = await context.Scheduler.GetTriggersOfJob(context.JobDetail.Key, cancellationToken).ConfigureAwait(false);
                if (triggers.Count > 1)
                {
                    Logger?.LogDebug("Not sending the schedule of trigger '{0}'. Job '{1}' has more than one trigger. " +
                                     "Set '{2}' in each trigger's data map to give each its own monitor.",
                        trigger.Key, context.JobDetail.Key, MonitorSlugKey);
                    return null;
                }
            }

            return configureMonitorOptions;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to read the schedule of trigger '{0}'.", trigger.Key);
            return null;
        }
    }

    internal static string? GetMonitorSlug(IJobExecutionContext context)
    {
        if (context.MergedJobDataMap.TryGetValue(MonitorSlugKey, out var value)
            && value is string monitorSlug
            && !string.IsNullOrWhiteSpace(monitorSlug))
        {
            return monitorSlug;
        }

        var key = context.JobDetail.Key;
        return ToSlug(key.Group == JobKey.DefaultGroup ? key.Name : key.Group + "." + key.Name);
    }

    // Produces a slug that Sentry's own slugify leaves unchanged
    internal static string? ToSlug(string value)
    {
        var slug = new StringBuilder(value.Length);
        foreach (var c in value.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            {
                slug.Append(c);
            }
            else if (slug.Length > 0 && slug[slug.Length - 1] != '-')
            {
                slug.Append('-');
            }
        }

        var result = slug.ToString().Trim('-', '_');
        if (result.Length > MaxSlugLength)
        {
            result = result.Substring(0, MaxSlugLength).Trim('-', '_');
        }

        return result.Length > 0 ? result : null;
    }
}
