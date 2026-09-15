using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryCronInformation
{
    public SentryCronInformation(IJob job)
    {
        var jobType = job.GetType();
        var monitorAttribute = jobType.GetCustomAttribute<SentryCronMonitorSlugAttribute>();

        ShouldWriteStatusToSentry = monitorAttribute is not null;
        MonitorSlug = monitorAttribute?.MonitorSlug ?? jobType.Name;
    }

    public string MonitorSlug { get; }

    public bool ShouldWriteStatusToSentry { get; }

    internal bool WarningShownForSecondsParameterIssue { get; set; }
}
