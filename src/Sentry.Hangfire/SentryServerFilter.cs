using Hangfire.Server;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

internal class SentryServerFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;

    public SentryServerFilter() : this(HubAdapter.Instance)
    { }

    internal SentryServerFilter(IHub hub, IDiagnosticLogger? logger = null)
    {
        _hub = hub;
        _logger = logger ?? _hub.GetSentryOptions()?.DiagnosticLogger;
    }

    public void OnPerforming(PerformingContext context)
    {
        var monitorSlug = context.GetJobParameter<string>(SentryMonitorSlugKey);
        if(monitorSlug is null)
        {
            var jobType = context.BackgroundJob.Job.Type;
            var jobMethod = context.BackgroundJob.Job.Method;
            _logger?.LogWarning("Skipping creating a check-in for '{0}.{1}'. " +
                                "Failed to find Monitor Slug the job. You can set the monitor slug " +
                                "by setting the 'SentryMonitorSlug' attribute.", jobType, jobMethod);
            return;
        }

        var checkInId = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, CheckInStatus.InProgress));
        context.Items.Add(SentryCheckInIdKey, checkInId);
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

        _ = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, status, checkInId));
    }
}
