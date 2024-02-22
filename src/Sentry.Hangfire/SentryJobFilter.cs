using Hangfire.Server;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

internal class SentryJobFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;

    // Wrapping the context getter and setter for testing
    private readonly Func<PerformContext?, string, string?> _getMonitorSlug;
    private readonly Func<PerformContext?, string, SentryId?> _getCheckInId;
    private readonly Action<PerformContext?, string, SentryId> _setCheckInId;
    private readonly Func<PerformedContext?, bool> _hasException;

    public SentryJobFilter() : this(HubAdapter.Instance, GetMonitorSlug, GetCheckInId, SetCheckInId, HasException)
    { }

    internal SentryJobFilter(IHub hub,
        Func<PerformContext?, string, string?> getMonitorSlug,
        Func<PerformContext?, string, SentryId?> getCheckInId,
        Action<PerformContext?, string, SentryId> setCheckInId,
        Func<PerformedContext?, bool> hasException)
    {
        _hub = hub;
        _logger = _hub.GetSentryOptions()?.DiagnosticLogger;

        _getMonitorSlug = getMonitorSlug;
        _getCheckInId = getCheckInId;
        _setCheckInId = setCheckInId;
        _hasException = hasException;
    }

    public void OnPerforming(PerformingContext? context)
    {
        if (_getMonitorSlug(context, SentryMonitorSlugKey) is not { } monitorSlug)
        {
            var jobName = context?.BackgroundJob.Job.ToString();
            _logger?.LogWarning("Skipping creating a check-in for '{0}'. " +
                                "Failed to find Monitor Slug the job. You can set the monitor slug " +
                                "by setting the 'SentryMonitorSlug' attribute.", jobName);
            return;
        }

        var checkInId = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, CheckInStatus.InProgress));
        _setCheckInId(context, SentryCheckInIdKey, checkInId);
    }

    public void OnPerformed(PerformedContext? context)
    {
        if (_getMonitorSlug(context, SentryMonitorSlugKey) is not { } monitorSlug)
        {
            return;
        }

        if (_getCheckInId(context, SentryCheckInIdKey) is not { } checkInId)
        {
            return;
        }

        var status = CheckInStatus.Ok;
        if (_hasException(context))
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
