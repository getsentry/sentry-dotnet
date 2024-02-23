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
            var jobName = context.BackgroundJob.Job.ToString();
            _logger?.LogWarning("Skipping creating a check-in for '{0}'. " +
                                "Failed to find Monitor Slug the job. You can set the monitor slug " +
                                "by setting the 'SentryMonitorSlug' attribute.", jobName);
            return;
        }

        var checkInId = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, CheckInStatus.InProgress));
        // context.SetJobParameter(SentryCheckInIdKey, checkInId);
    }

    public void OnPerformed(PerformedContext context)
    {
        var monitorSlug = context.GetJobParameter<string>(SentryMonitorSlugKey);
        if(monitorSlug is null)
        {
            return;
        }

        var checkInId = context.GetJobParameter<SentryId>(SentryCheckInIdKey);
        if (checkInId.Equals(SentryId.Empty))
        {
            return;
        }

        var status = CheckInStatus.Ok;
        if (context.Exception is not null)
        {
            status = CheckInStatus.Error;
        }

        _ = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, status, checkInId));
    }
}
