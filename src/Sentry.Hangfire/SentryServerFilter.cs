using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry.Hangfire;

internal class SentryServerFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";
    internal const string RecurringJobIdKey = "RecurringJobId";

    // How long the slugs of all recurring jobs are reused before they're read from storage again
    internal static readonly TimeSpan RecurringJobSlugsTtl = TimeSpan.FromMinutes(1);

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;
    private readonly SentryHangfireOptions _options;
    private readonly ISystemClock _clock;
    private readonly object _recurringJobSlugsLock = new();
    private RecurringJobSlugs? _recurringJobSlugs;

    internal SentryHangfireOptions Options => _options;

    public SentryServerFilter() : this(null, null)
    { }

    internal SentryServerFilter(IHub? hub, IDiagnosticLogger? logger, SentryHangfireOptions? options = null,
        ISystemClock? clock = null)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? new SentryHangfireOptions();
        _clock = clock ?? SystemClock.Clock;
#pragma warning disable CS0618 // Type or member is obsolete
        _logger = logger ?? _hub.GetInternalSentryOptions()?.DiagnosticLogger;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public void OnPerforming(PerformingContext context)
    {
        var monitorSlug = context.GetJobParameter<string>(SentryMonitorSlugKey);
        if (monitorSlug is null)
        {
            var jobType = context.BackgroundJob.Job.Type;
            var jobMethod = context.BackgroundJob.Job.Method;
            _logger?.LogDebug("Skipping creating a check-in for '{0}.{1}'. " +
                                "Failed to find Monitor Slug for the job. You can set the monitor slug " +
                                "by setting the 'SentryMonitorSlug' attribute.", jobType, jobMethod);
            return;
        }

        var checkInId = CaptureInProgressCheckIn(context, monitorSlug);

        // Note that we may be overwriting context.Items[SentryCheckInIdKey] here, which is intentional. If that happens
        // then implicitly OnPerforming was called previously with the same context, but we never made it to OnPerformed
        // This might happen if a Hangfire job failed at least once, with automatic retries configured.
        context.Items[SentryCheckInIdKey] = checkInId;
    }

    private SentryId CaptureInProgressCheckIn(PerformingContext context, string monitorSlug)
    {
        Action<SentryMonitorOptions>? configureMonitorOptions = null;
        if (_options.SendMonitorConfig && GetRecurringJobSchedule(context, monitorSlug) is (var crontab, var timeZone))
        {
            configureMonitorOptions = options =>
            {
                options.Interval(crontab);
                options.TimeZone = timeZone;
            };
        }

        // Created here, so the final check-in has the same ID even when this one isn't sent
        var checkInId = SentryId.Create();
        _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress, checkInId, configureMonitorOptions: configureMonitorOptions);
        return checkInId;
    }

    private (string Crontab, string TimeZone)? GetRecurringJobSchedule(PerformingContext context, string monitorSlug)
    {
        string? recurringJobId = null;
        try
        {
            recurringJobId = context.GetJobParameter<string>(RecurringJobIdKey);
            if (string.IsNullOrEmpty(recurringJobId))
            {
                return null;
            }

            var recurringJob = context.Connection.GetRecurringJobs([recurringJobId]).SingleOrDefault();
            if (recurringJob is null || recurringJob.Removed)
            {
                _logger?.LogDebug("Not sending the schedule of recurring job '{0}'. The job no longer exists.", recurringJobId);
                return null;
            }

            var crontab = ToCrontab(recurringJob.Cron);
            var timeZone = ToIanaTimeZoneId(recurringJob.TimeZoneId);
            if (crontab is null || timeZone is null)
            {
                _logger?.LogDebug("Not sending the schedule of recurring job '{0}'. Sentry doesn't support " +
                                  "the cron expression '{1}' with time zone '{2}'.", recurringJobId, recurringJob.Cron, recurringJob.TimeZoneId);
                return null;
            }

            var conflicting = GetRecurringJobSlugs(context.Connection, recurringJobId!)
                .WithDifferentSchedule(monitorSlug, recurringJob.Cron, recurringJob.TimeZoneId);
            if (conflicting.Count > 0)
            {
                _logger?.LogDebug("Not sending the schedule of recurring job '{0}'. Recurring jobs '{1}' use the same " +
                                  "monitor slug '{2}' with a different schedule. Give each its own monitor slug.",
                    recurringJobId, string.Join("', '", conflicting), monitorSlug);
                return null;
            }

            return (crontab, timeZone);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to read the schedule of recurring job '{0}'.", recurringJobId);
            return null;
        }
    }

    // Reading every recurring job takes one storage query per job, so the result is reused for a minute.
    private RecurringJobSlugs GetRecurringJobSlugs(IStorageConnection connection, string recurringJobId)
    {
        lock (_recurringJobSlugsLock)
        {
            var now = _clock.GetUtcNow();
            if (_recurringJobSlugs is not { } slugs || slugs.ReadAt + RecurringJobSlugsTtl <= now
                || !slugs.Contains(recurringJobId))
            {
                slugs = RecurringJobSlugs.Read(connection, now);
                _recurringJobSlugs = slugs;
            }

            return slugs;
        }
    }

    private sealed class RecurringJobSlugs
    {
        private readonly Dictionary<string, List<(string Id, string? Cron, string TimeZoneId)>> _jobsBySlug;
        private readonly HashSet<string> _ids;

        public DateTimeOffset ReadAt { get; }

        private RecurringJobSlugs(DateTimeOffset readAt,
            Dictionary<string, List<(string Id, string? Cron, string TimeZoneId)>> jobsBySlug, HashSet<string> ids)
        {
            ReadAt = readAt;
            _jobsBySlug = jobsBySlug;
            _ids = ids;
        }

        public static RecurringJobSlugs Read(IStorageConnection connection, DateTimeOffset readAt)
        {
            var jobsBySlug = new Dictionary<string, List<(string Id, string? Cron, string TimeZoneId)>>();
            var ids = new HashSet<string>();
            foreach (var recurringJob in connection.GetRecurringJobs())
            {
                ids.Add(recurringJob.Id);
                if (recurringJob.Removed || recurringJob.Job is null || GetMonitorSlug(recurringJob.Job) is not { } slug)
                {
                    continue;
                }

                if (!jobsBySlug.TryGetValue(slug, out var jobs))
                {
                    jobs = new List<(string Id, string? Cron, string TimeZoneId)>();
                    jobsBySlug[slug] = jobs;
                }
                jobs.Add((recurringJob.Id, recurringJob.Cron, NormalizeTimeZoneId(recurringJob.TimeZoneId)));
            }

            return new RecurringJobSlugs(readAt, jobsBySlug, ids);
        }

        public bool Contains(string recurringJobId) => _ids.Contains(recurringJobId);

        public List<string> WithDifferentSchedule(string monitorSlug, string? cron, string? timeZoneId)
        {
            if (!_jobsBySlug.TryGetValue(monitorSlug, out var jobs))
            {
                return new List<string>();
            }

            var normalizedTimeZoneId = NormalizeTimeZoneId(timeZoneId);
            return jobs
                .Where(job => job.Cron != cron || job.TimeZoneId != normalizedTimeZoneId)
                .Select(job => job.Id)
                .ToList();
        }

        private static string? GetMonitorSlug(Job job) =>
            JobFilterProviders.Providers.GetFilters(job)
                .Select(filter => filter.Instance)
                .OfType<SentryMonitorSlugAttribute>()
                .LastOrDefault()?.MonitorSlug;

        private static string NormalizeTimeZoneId(string? timeZoneId) =>
            string.IsNullOrEmpty(timeZoneId) ? "UTC" : timeZoneId!;
    }

    // Hangfire also accepts a leading seconds field, which Sentry doesn't support.
    internal static string? ToCrontab(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
        {
            return null;
        }

        var fields = cron!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length == 6
            && int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            && seconds < 60)
        {
            fields = fields.Skip(1).ToArray();
        }

        if (fields.Length != 5)
        {
            return null;
        }

        // Hangfire runs a job only on days that match both day fields, while Sentry runs it on days that match
        // either one unless one of them starts with '*'.
        if (!fields[2].StartsWith("*") && !fields[4].StartsWith("*"))
        {
            return null;
        }

        var crontab = string.Join(" ", fields);
        return CrontabValidator.IsValid(crontab) && !HasReversedRange(fields) ? crontab : null;
    }

    private static readonly string[] DayNames = ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];

    // Sentry rejects ranges whose start is after their end, such as 5-1 or SAT-SUN
    private static bool HasReversedRange(string[] fields)
    {
        foreach (var field in fields)
        {
            foreach (var item in field.Split(','))
            {
                var range = item.Split('/')[0].Split('-');
                if (range.Length == 2 && ToNumber(range[0]) > ToNumber(range[1]))
                {
                    return true;
                }
            }
        }

        return false;

        static int ToNumber(string value) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                ? number
                : Array.FindIndex(DayNames, name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? ToIanaTimeZoneId(string? timeZoneId)
    {
        // Hangfire's default is TimeZoneInfo.Utc, whose ID is "UTC" on every platform
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId == "UTC")
        {
            return "UTC";
        }

#if NET6_0_OR_GREATER
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId!);
        }
        catch (Exception)
        {
            return null;
        }

        if (timeZone.HasIanaId)
        {
            return timeZone.Id;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId) ? ianaId : null;
#else
        // .NET Framework can neither look up nor convert IANA IDs, so only pass on IDs that look like one
        return timeZoneId!.Contains("/") ? timeZoneId : null;
#endif
    }

    public void OnPerformed(PerformedContext context)
    {
        var monitorSlug = context.GetJobParameter<string>(SentryMonitorSlugKey);
        if (monitorSlug is null)
        {
            return;
        }

        if (!context.Items.TryGetValue(SentryCheckInIdKey, out var checkInIdObject) || checkInIdObject is not SentryId checkInId)
        {
            return;
        }

        var status = context.Exception is null ? CheckInStatus.Ok : CheckInStatus.Error;
        var duration = DateTime.UtcNow - context.BackgroundJob.CreatedAt;

        _ = _hub.CaptureCheckIn(monitorSlug, status, checkInId, duration: duration);
    }
}
