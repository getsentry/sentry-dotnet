using Hangfire.Server;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

internal class SentryJobFilter : IServerFilter
{
    internal const string SentryMonitorSlugKey = "SentryMonitorSlug";
    internal const string SentryCheckInIdKey = "SentryCheckInIdKey";

    private readonly IHub _hub;
    private readonly IDiagnosticLogger? _logger;

    public SentryJobFilter() : this(() => HubAdapter.Instance)
    { }

    internal SentryJobFilter(Func<IHub> hubAccessor)
    {
        _hub = hubAccessor.Invoke();
        _logger = _hub.GetSentryOptions()?.DiagnosticLogger;
    }

    public void OnPerforming(PerformingContext context)
    {
        OnPerformingInternal(context, GetJobParameter, SetJobParameter);
    }

    public void OnPerformed(PerformedContext context)
    {
        OnPerformedInternal(context, GetJobParameter, HasException);
    }

    internal void OnPerformingInternal(PerformingContext? context,
        Func<PerformContext?, string, object?> getJobParameterFunc,
        Action<PerformContext?, string, SentryId> setJobParameter)
    {
        if (getJobParameterFunc(context, SentryMonitorSlugKey) is not string monitorSlug)
        {
            var jobName = context?.BackgroundJob.Job.ToString();
            _logger?.LogWarning("Skipping creating a check-in for '{0}'. " +
                                "Failed to find Monitor Slug the job. You can set the monitor slug " +
                                "by setting the 'SentryMonitorSlug' attribute.", jobName);
            return;
        }

        var checkInId = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, CheckInStatus.InProgress));
        setJobParameter(context, SentryCheckInIdKey, checkInId);
    }

    internal void OnPerformedInternal(PerformedContext? context,
        Func<PerformContext?, string, object?> getJobParameterFunc,
        Func<PerformedContext?, bool> hasExceptionFunc)
    {
        if (getJobParameterFunc(context, SentryMonitorSlugKey) is not string monitorSlug)
        {
            return;
        }

        if (getJobParameterFunc(context, SentryCheckInIdKey) is not SentryId checkInId)
        {
            return;
        }

        var status = CheckInStatus.Ok;
        if (hasExceptionFunc(context))
        {
            status = CheckInStatus.Error;
        }

        _ = _hub.CaptureCheckIn(new SentryCheckIn(monitorSlug, status, checkInId));
    }

    private static object? GetJobParameter(PerformContext? context, string key)
    {
        return context?.GetJobParameter<object>(key);
    }

    private static void SetJobParameter(PerformContext? context, string key, SentryId value)
    {
        context?.SetJobParameter(key, value);
    }

    private static bool HasException(PerformedContext? context)
    {
        return context?.Exception is not null;
    }
}
