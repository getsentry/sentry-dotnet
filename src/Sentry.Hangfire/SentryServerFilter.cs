using Hangfire.Server;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

internal class SentryServerFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;

    public SentryServerFilter() : this(null, null)
    { }

    internal SentryServerFilter(IHub? hub, IDiagnosticLogger? logger)
    {
        _hub = hub ?? HubAdapter.Instance;
        _logger = logger ?? _hub.GetSentryOptions()?.DiagnosticLogger;
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

        var checkInId = _hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress);

        // Note that we may be overwriting context.Items[SentryCheckInIdKey] here, which is intentional. If that happens
        // then implicitly OnPerforming was called previously with the same context, but we never made it to OnPerformed
        // This might happen if a Hangfire job failed at least once, with automatic retries configured.
        context.Items[SentryCheckInIdKey] = checkInId;
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
