using Hangfire.Server;
using Hangfire.Storage;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Hangfire;

internal class SentryServerFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";
    internal const string RecurringJobIdKey = "RecurringJobId";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;
    private readonly SentryHangfireOptions _options;

    internal SentryHangfireOptions Options => _options;

    public SentryServerFilter() : this(null, null)
    { }

    internal SentryServerFilter(IHub? hub, IDiagnosticLogger? logger, SentryHangfireOptions? options = null)
    {
        _hub = hub ?? HubAdapter.Instance;
        _options = options ?? new SentryHangfireOptions();
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
        if (_options.SendRecurringJobSchedule && GetRecurringJobSchedule(context) is (var crontab, var timeZone))
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

    private (string Crontab, string TimeZone)? GetRecurringJobSchedule(PerformingContext context)
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

            return (crontab, timeZone);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to read the schedule of recurring job '{0}'.", recurringJobId);
            return null;
        }
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
