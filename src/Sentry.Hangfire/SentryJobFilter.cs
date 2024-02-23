using Hangfire.Server;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

internal class SentryJobFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;

    public SentryJobFilter() : this(HubAdapter.Instance)
    { }

    internal SentryJobFilter(IHub hub)
    {
        _hub = hub;
        _logger = _hub.GetSentryOptions()?.DiagnosticLogger;
    }

    public void OnPerforming(PerformingContext context)
    {
        var success = OnPerformingInternal(context.Items);
        if (!success)
        {
            var jobName = context?.BackgroundJob.Job.ToString();
            _logger?.LogWarning("Skipping creating a check-in for '{0}'. " +
                                "Failed to find Monitor Slug the job. You can set the monitor slug " +
                                "by setting the 'SentryMonitorSlug' attribute.", jobName);
        }
    }

    internal bool OnPerformingInternal(IDictionary<string, object> items)
    {
        items.TryGetValue(SentryMonitorSlugKey, out var monitorSlugObject);
        if(monitorSlugObject is not string monitorSlug)
        {
            return false;
        }

        var checkInId = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, CheckInStatus.InProgress));
        items.Add(SentryCheckInIdKey, checkInId);
        return true;
    }

    public void OnPerformed(PerformedContext context)
    {
        var hasException = context.Exception is not null;
        OnPerformedInternal(context.Items, hasException);
    }

    internal void OnPerformedInternal(IDictionary<string, object> items, bool hasException)
    {
        items.TryGetValue(SentryMonitorSlugKey, out var monitorSlugObject);
        if(monitorSlugObject is not string monitorSlug)
        {
            return;
        }

        items.TryGetValue(SentryMonitorSlugKey, out var checkInIdObject);
        if (checkInIdObject is not SentryId checkInId)
        {
            return;
        }

        var status = CheckInStatus.Ok;
        if (hasException)
        {
            status = CheckInStatus.Error;
        }

        _ = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, status, checkInId));
    }

    private static string? GetMonitorSlug(PerformContext? context, string key)
    {
        return context?.GetJobParameter<string>(key);
    }

    private static SentryId? GetCheckInId(PerformContext? context, string key)
    {
        return context?.GetJobParameter<SentryId>(key);
    }

    private static void SetCheckInId(PerformContext? context, string key, SentryId value)
    {
        context?.SetJobParameter(key, value);
    }

    private static bool HasException(PerformedContext? context)
    {
        return context?.Exception is not null;
    }
}
