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

    // The job each slug was first used by: its key, or its type when it has no identity
    private readonly ConcurrentDictionary<string, string> _slugOwners = new();
    private readonly ConcurrentDictionary<string, byte> _sharedSlugs = new();
    private readonly ConcurrentDictionary<Type, byte> _jobTypesWithoutIdentity = new();

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
        string? monitorSlug;
        Action<SentryMonitorOptions>? configureMonitorOptions;
        try
        {
            var jobType = QuartzApi.GetJobType(context.JobDetail);
            var attribute = Attributes.GetOrAdd(jobType, type => type.GetCustomAttribute<SentryCronMonitorSlugAttribute>());
            if (attribute is null)
            {
                return null;
            }

            monitorSlug = ResolveMonitorSlug(context, jobType, attribute);
            if (monitorSlug is null)
            {
                Logger?.LogDebug("Skipping the check-in for job '{0}'. Its name has no characters a monitor slug can use. " +
                                 "Set a monitor slug in its SentryCronMonitorSlug attribute.", context.JobDetail.Key);
                return null;
            }

            configureMonitorOptions = _options.SendMonitorConfig && attribute.SendMonitorConfig
                ? await GetMonitorConfigAsync(context, cancellationToken).ConfigureAwait(false)
                : null;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to capture the check-in for job '{0}'.", context.JobDetail.Key);
            return null;
        }

        // Created here, so the final check-in has the same ID even when this one isn't sent
        var checkIn = new JobCheckIn(monitorSlug, SentryId.Create());
        try
        {
            _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress, checkIn.Id, configureMonitorOptions: configureMonitorOptions);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to capture the in-progress check-in for job '{0}'.", context.JobDetail.Key);
        }

        return checkIn;
    }

    public void Finish(JobCheckIn checkIn, bool failed, TimeSpan duration)
    {
        try
        {
            _hub.CaptureCheckIn(checkIn.MonitorSlug, failed ? CheckInStatus.Error : CheckInStatus.Ok, checkIn.Id, duration);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to capture the check-in for monitor '{0}'.", checkIn.MonitorSlug);
        }
    }

    // Returns null when there's no schedule to send
    private async Task<Action<SentryMonitorOptions>?> GetMonitorConfigAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var monitorOptions = new SentryMonitorOptions();
        if (_options.ConfigureMonitorOptions is { } configure)
        {
            try
            {
                configure(context, monitorOptions);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "ConfigureMonitorOptions threw for job '{0}'.", context.JobDetail.Key);
            }
        }

        if (!monitorOptions.HasSchedule
            && await GetTriggerScheduleAsync(context, cancellationToken).ConfigureAwait(false) is { } schedule)
        {
            schedule(monitorOptions);
        }

        return monitorOptions.HasSchedule ? monitorOptions.CopyTo : null;
    }

    private async Task<Action<SentryMonitorOptions>?> GetTriggerScheduleAsync(
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

            return schedule;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to read the schedule of trigger '{0}'.", trigger.Key);
            return null;
        }
    }

    private string? ResolveMonitorSlug(IJobExecutionContext context, Type jobType, SentryCronMonitorSlugAttribute attribute)
    {
        var monitorSlug = GetMonitorSlug(context, attribute, out var fromJobType);
        if (monitorSlug is null)
        {
            return null;
        }

        if (fromJobType && _jobTypesWithoutIdentity.TryAdd(jobType, 0))
        {
            Logger?.LogWarning("Job `{0}` has no identity, so its monitor slug is `{1}`. Give the job an identity " +
                               "(WithIdentity(...)) or set a slug ([SentryCronMonitorSlug(\"…\")]) to keep the monitor stable.",
                jobType.FullName, monitorSlug);
        }

        var key = context.JobDetail.Key;
        var owner = HasIdentity(key) ? key.ToString() : jobType.FullName ?? jobType.Name;
        var firstOwner = _slugOwners.GetOrAdd(monitorSlug, owner);
        if (firstOwner != owner && _sharedSlugs.TryAdd(monitorSlug, 0))
        {
            Logger?.LogWarning("Jobs '{0}' and '{1}' both use the monitor slug '{2}', so their check-ins go to the same " +
                               "monitor. Give each job its own identity or slug.", firstOwner, owner, monitorSlug);
        }

        return monitorSlug;
    }

    internal static string? GetMonitorSlug(IJobExecutionContext context, SentryCronMonitorSlugAttribute attribute) =>
        GetMonitorSlug(context, attribute, out _);

    private static string? GetMonitorSlug(
        IJobExecutionContext context,
        SentryCronMonitorSlugAttribute attribute,
        out bool fromJobType)
    {
        fromJobType = false;
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
        if (HasIdentity(key))
        {
            return ToSlug(key.Group == JobKey.DefaultGroup ? key.Name : key.Group + "." + key.Name);
        }

        fromJobType = true;
        return ToSlug(QuartzApi.GetJobType(context.JobDetail).Name);
    }

    // Quartz names a job built without an identity with a new GUID
    internal static bool HasIdentity(JobKey key) => !Guid.TryParse(key.Name, out _);

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
