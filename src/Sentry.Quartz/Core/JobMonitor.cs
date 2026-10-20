using Quartz;
using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Quartz;

// Captures the check-ins of jobs with SentryMonitorSlugAttribute
internal sealed class JobMonitor
{
    private const int MaxSlugLength = 50;

    // How long the schedules of all monitored triggers are reused before they're read from the scheduler again
    internal static readonly TimeSpan TriggerSchedulesTtl = TimeSpan.FromMinutes(1);

    private static readonly ConcurrentDictionary<Type, SentryMonitorSlugAttribute?> Attributes = new();

    private readonly SentryQuartzOptions _options;
    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;
    private readonly ISystemClock _clock;
    private readonly SemaphoreSlim _triggerSchedulesLock = new(1, 1);
    private TriggerSchedules? _triggerSchedules;

    public JobMonitor(SentryQuartzOptions options, IHub hub, IDiagnosticLogger? logger = null, ISystemClock? clock = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hub = hub;
        _logger = logger;
        _clock = clock ?? SystemClock.Clock;
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
            var attribute = GetAttribute(jobType);
            if (attribute is null)
            {
                return null;
            }

            monitorSlug = ResolveMonitorSlug(context, jobType, attribute);
            if (monitorSlug is null)
            {
                Logger?.LogDebug("Skipping the check-in for job '{0}'. Its name has no characters a monitor slug can use. " +
                                 "Set a monitor slug in its SentryMonitorSlug attribute.", context.JobDetail.Key);
                return null;
            }

            configureMonitorOptions = _options.SendMonitorConfig && attribute.SendMonitorConfig
                ? await GetMonitorConfigAsync(context, monitorSlug, cancellationToken).ConfigureAwait(false)
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
        string monitorSlug,
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
                // Don't send whatever the callback set before it threw
                monitorOptions = new SentryMonitorOptions();
                Logger?.LogError(e, "ConfigureMonitorOptions threw for job '{0}'.", context.JobDetail.Key);
            }
        }

        if (!monitorOptions.HasSchedule
            && await GetTriggerScheduleAsync(context, monitorSlug, cancellationToken).ConfigureAwait(false) is { } schedule)
        {
            schedule.Apply(monitorOptions);
        }

        return monitorOptions.HasSchedule ? monitorOptions.CopyTo : null;
    }

    private async Task<MonitorSchedule?> GetTriggerScheduleAsync(
        IJobExecutionContext context,
        string monitorSlug,
        CancellationToken cancellationToken)
    {
        var trigger = context.Trigger;
        try
        {
            var schedule = TriggerSchedule.ToSchedule(trigger);
            if (schedule is null)
            {
                Logger?.LogDebug("Not sending the schedule of trigger '{0}'. Sentry can't represent it.", trigger.Key);
                return null;
            }

            var triggerSchedules = await GetTriggerSchedulesAsync(context.Scheduler, trigger.Key, cancellationToken)
                .ConfigureAwait(false);
            var conflicting = triggerSchedules.WithDifferentSchedule(monitorSlug, trigger.Key, schedule);
            if (conflicting.Count > 0)
            {
                Logger?.LogDebug("Not sending the schedule of trigger '{0}'. Triggers '{1}' use the same monitor slug " +
                                 "'{2}' with a different schedule. Give each its own monitor slug.",
                    trigger.Key, string.Join("', '", conflicting), monitorSlug);
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

    // Reading every trigger takes several scheduler queries, so the result is reused for a minute.
    private async Task<TriggerSchedules> GetTriggerSchedulesAsync(
        IScheduler scheduler,
        TriggerKey triggerKey,
        CancellationToken cancellationToken)
    {
        await _triggerSchedulesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _clock.GetUtcNow();
            if (_triggerSchedules is not { } schedules || schedules.ReadAt + TriggerSchedulesTtl <= now
                || schedules.Scheduler != scheduler || !schedules.Contains(triggerKey))
            {
                schedules = await TriggerSchedules.ReadAsync(scheduler, now, cancellationToken).ConfigureAwait(false);
                _triggerSchedules = schedules;
            }

            return schedules;
        }
        finally
        {
            _triggerSchedulesLock.Release();
        }
    }

    private sealed class TriggerSchedules
    {
        private readonly Dictionary<string, List<(TriggerKey Key, MonitorSchedule Schedule)>> _triggersBySlug;
        private readonly HashSet<TriggerKey> _keys;

        public IScheduler Scheduler { get; }

        public DateTimeOffset ReadAt { get; }

        private TriggerSchedules(IScheduler scheduler, DateTimeOffset readAt,
            Dictionary<string, List<(TriggerKey Key, MonitorSchedule Schedule)>> triggersBySlug, HashSet<TriggerKey> keys)
        {
            Scheduler = scheduler;
            ReadAt = readAt;
            _triggersBySlug = triggersBySlug;
            _keys = keys;
        }

        public static async Task<TriggerSchedules> ReadAsync(IScheduler scheduler, DateTimeOffset readAt,
            CancellationToken cancellationToken)
        {
            var triggersBySlug = new Dictionary<string, List<(TriggerKey Key, MonitorSchedule Schedule)>>();
            var keys = new HashSet<TriggerKey>();
            var triggers = await QuartzApi.GetTriggersAsync(scheduler, IsMonitored, cancellationToken).ConfigureAwait(false);
            foreach (var (job, trigger) in triggers)
            {
                keys.Add(trigger.Key);

                // Triggers whose schedule isn't sent don't change the monitor
                var attribute = GetAttribute(QuartzApi.GetJobType(job))!;
                if (!attribute.SendMonitorConfig
                    || TriggerSchedule.ToSchedule(trigger) is not { } schedule
                    || GetMonitorSlug(job, trigger, attribute) is not { } slug)
                {
                    continue;
                }

                if (!triggersBySlug.TryGetValue(slug, out var slugTriggers))
                {
                    slugTriggers = new List<(TriggerKey Key, MonitorSchedule Schedule)>();
                    triggersBySlug[slug] = slugTriggers;
                }
                slugTriggers.Add((trigger.Key, schedule));
            }

            return new TriggerSchedules(scheduler, readAt, triggersBySlug, keys);
        }

        public bool Contains(TriggerKey triggerKey) => _keys.Contains(triggerKey);

        public List<TriggerKey> WithDifferentSchedule(string monitorSlug, TriggerKey triggerKey, MonitorSchedule schedule)
        {
            if (!_triggersBySlug.TryGetValue(monitorSlug, out var triggers))
            {
                return new List<TriggerKey>();
            }

            return triggers
                .Where(trigger => !trigger.Key.Equals(triggerKey) && trigger.Schedule != schedule)
                .Select(trigger => trigger.Key)
                .ToList();
        }

        private static bool IsMonitored(IJobDetail job) => GetAttribute(QuartzApi.GetJobType(job)) is not null;
    }

    private static SentryMonitorSlugAttribute? GetAttribute(Type jobType) =>
        Attributes.GetOrAdd(jobType, type => type.GetCustomAttribute<SentryMonitorSlugAttribute>());

    private string? ResolveMonitorSlug(IJobExecutionContext context, Type jobType, SentryMonitorSlugAttribute attribute)
    {
        var monitorSlug = GetMonitorSlug(context, attribute, out var fromJobType);
        if (monitorSlug is null)
        {
            return null;
        }

        if (fromJobType)
        {
            Logger?.LogDebug("Job `{0}` has no identity, so its monitor slug is `{1}`. Give the job an identity " +
                             "(WithIdentity(...)) or set a slug ([SentryMonitorSlug(\"…\")]) to keep the monitor stable.",
                jobType.FullName, monitorSlug);
        }

        return monitorSlug;
    }

    internal static string? GetMonitorSlug(IJobExecutionContext context, SentryMonitorSlugAttribute attribute) =>
        GetMonitorSlug(context, attribute, out _);

    private static string? GetMonitorSlug(
        IJobExecutionContext context,
        SentryMonitorSlugAttribute attribute,
        out bool fromJobType)
    {
        context.MergedJobDataMap.TryGetValue(SentryMonitorSlugAttribute.JobDataKey, out var dataMapSlug);
        return GetMonitorSlug(context.JobDetail, dataMapSlug, attribute, out fromJobType);
    }

    // The slug of a run of the job by the trigger, as its merged JobDataMap would give
    private static string? GetMonitorSlug(IJobDetail job, ITrigger trigger, SentryMonitorSlugAttribute attribute)
    {
        if (!trigger.JobDataMap.TryGetValue(SentryMonitorSlugAttribute.JobDataKey, out var dataMapSlug))
        {
            job.JobDataMap.TryGetValue(SentryMonitorSlugAttribute.JobDataKey, out dataMapSlug);
        }

        return GetMonitorSlug(job, dataMapSlug, attribute, out _);
    }

    private static string? GetMonitorSlug(
        IJobDetail job,
        object? dataMapValue,
        SentryMonitorSlugAttribute attribute,
        out bool fromJobType)
    {
        fromJobType = false;
        if (dataMapValue is string dataMapSlug && !string.IsNullOrWhiteSpace(dataMapSlug))
        {
            return dataMapSlug;
        }

        if (!string.IsNullOrWhiteSpace(attribute.MonitorSlug))
        {
            return attribute.MonitorSlug;
        }

        var key = job.Key;
        if (HasIdentity(key))
        {
            return ToSlug(key.Group == JobKey.DefaultGroup ? key.Name : key.Group + "." + key.Name);
        }

        fromJobType = true;
        return ToSlug(QuartzApi.GetJobType(job).Name);
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
