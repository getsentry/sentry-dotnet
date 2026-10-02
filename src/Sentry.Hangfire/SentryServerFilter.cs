using System.Globalization;
using Hangfire.Server;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

internal class SentryServerFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";
    internal const string RecurringJobIdKey = "RecurringJobId";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;
    private readonly SentryHangfireOptions _options;

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
        if (_options.SendRecurringJobSchedule && GetRecurringJobSchedule(context) is { } schedule)
        {
            var scheduleRejected = false;
            try
            {
                var checkInId = _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress, configureMonitorOptions: options =>
                {
                    try
                    {
                        options.Interval(schedule.Crontab);
                    }
                    catch
                    {
                        scheduleRejected = true;
                        throw;
                    }
                    options.TimeZone = schedule.TimeZone;
                });

                if (!scheduleRejected)
                {
                    return checkInId;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to capture a check-in with the monitor config for '{0}'.", monitorSlug);
            }

            if (scheduleRejected)
            {
                _logger?.LogDebug("Sending the check-in for '{0}' without a monitor config. " +
                                  "Sentry doesn't support the schedule '{1}'.", monitorSlug, schedule.Crontab);
            }
        }

        return _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress);
    }

    private (string Crontab, string TimeZone)? GetRecurringJobSchedule(PerformingContext context)
    {
        try
        {
            var recurringJobId = context.GetJobParameter<string>(RecurringJobIdKey);
            if (string.IsNullOrEmpty(recurringJobId))
            {
                return null;
            }

            var recurringJob = context.Connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
            if (recurringJob is null || !recurringJob.TryGetValue("Cron", out var cron))
            {
                return null;
            }

            recurringJob.TryGetValue("TimeZoneId", out var timeZoneId);

            var crontab = ToCrontab(cron);
            var timeZone = ToIanaTimeZoneId(timeZoneId);
            if (crontab is null || timeZone is null)
            {
                _logger?.LogDebug("Not sending the schedule of recurring job '{0}'. Sentry doesn't support " +
                                  "the cron expression '{1}' with time zone '{2}'.", recurringJobId, cron, timeZoneId);
                return null;
            }

            return (crontab, timeZone);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to read the schedule of the recurring job.");
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
        return fields.Length switch
        {
            5 => string.Join(" ", fields),
            6 when int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) && seconds < 60
                => string.Join(" ", fields, 1, 5),
            _ => null
        };
    }

    internal static string? ToIanaTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return "UTC";
        }

#if NET6_0_OR_GREATER
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId))
        {
            return ianaId;
        }
#endif

        return timeZoneId == "UTC" || timeZoneId!.Contains("/") ? timeZoneId : null;
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
