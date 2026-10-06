using Quartz;
using Sentry.Extensibility;

namespace Sentry.Quartz;

// Captures the check-ins of jobs with SentryCronMonitorSlugAttribute
internal sealed class JobMonitor
{
    private const int MaxSlugLength = 50;

    private static readonly ConcurrentDictionary<Type, SentryCronMonitorSlugAttribute?> Attributes = new();

    private readonly SentryQuartzOptions _options;
    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;

    public JobMonitor(SentryQuartzOptions options, IHub hub, IDiagnosticLogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hub = hub;
        _logger = logger;
    }

    internal IDiagnosticLogger? Logger => _logger ?? _hub.GetSentryOptions()?.DiagnosticLogger;

    // Returns null when the job isn't monitored
    public async Task<JobCheckIn?> StartAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        try
        {
            var jobType = QuartzApi.GetJobType(context.JobDetail);
            var attribute = Attributes.GetOrAdd(jobType, type => type.GetCustomAttribute<SentryCronMonitorSlugAttribute>());
            if (attribute is null)
            {
                return null;
            }

            var monitorSlug = GetMonitorSlug(context, attribute);
            if (monitorSlug is null)
            {
                Logger?.LogDebug("Skipping the check-in for job '{0}'. Its key has no characters a monitor slug can use. " +
                                 "Set a monitor slug in its SentryCronMonitorSlug attribute.", context.JobDetail.Key);
                return null;
            }

            var configureMonitorOptions = _options.SendMonitorConfig && attribute.SendMonitorConfig
                ? await GetMonitorConfigAsync(context, cancellationToken).ConfigureAwait(false)
                : null;

            var checkInId = _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress, configureMonitorOptions: configureMonitorOptions);
            return new JobCheckIn(monitorSlug, checkInId);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to capture the check-in for job '{0}'.", context.JobDetail.Key);
            return null;
        }
    }

    public void Finish(JobCheckIn checkIn, bool failed, TimeSpan duration) =>
        _hub.CaptureCheckIn(checkIn.MonitorSlug, failed ? CheckInStatus.Error : CheckInStatus.Ok, checkIn.Id, duration);

    private async Task<Action<SentryMonitorOptions>?> GetMonitorConfigAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var trigger = context.Trigger;
        try
        {
            var schedule = TriggerSchedule.ToMonitorConfig(trigger);
            if (schedule is null)
            {
                Logger?.LogDebug("Not sending the schedule of trigger '{0}'. Sentry can't represent it.", trigger.Key);
                return null;
            }

            if (!trigger.JobDataMap.ContainsKey(SentryCronMonitorSlugAttribute.JobDataKey)
                && await QuartzApi.HasSeveralTriggersAsync(context, cancellationToken).ConfigureAwait(false))
            {
                Logger?.LogDebug("Not sending the schedule of trigger '{0}'. Job '{1}' has more than one trigger. " +
                                 "Set '{2}' in each trigger's data map to give each its own monitor.",
                    trigger.Key, context.JobDetail.Key, SentryCronMonitorSlugAttribute.JobDataKey);
                return null;
            }

            var configure = _options.ConfigureMonitorOptions;
            if (configure is null)
            {
                return schedule;
            }

            var jobDetail = context.JobDetail;
            return options =>
            {
                try
                {
                    configure(jobDetail, options);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "ConfigureMonitorOptions threw for job '{0}'.", jobDetail.Key);
                }

                if (!options.HasSchedule)
                {
                    schedule(options);
                }
            };
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to read the schedule of trigger '{0}'.", trigger.Key);
            return null;
        }
    }

    internal static string? GetMonitorSlug(IJobExecutionContext context, SentryCronMonitorSlugAttribute attribute)
    {
        if (context.MergedJobDataMap.TryGetValue(SentryCronMonitorSlugAttribute.JobDataKey, out var value)
            && value is string dataMapSlug
            && !string.IsNullOrWhiteSpace(dataMapSlug))
        {
            return dataMapSlug;
        }

        if (!string.IsNullOrWhiteSpace(attribute.MonitorSlug))
        {
            return attribute.MonitorSlug;
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

internal sealed class JobCheckIn(string monitorSlug, SentryId id)
{
    public string MonitorSlug { get; } = monitorSlug;

    public SentryId Id { get; } = id;
}
