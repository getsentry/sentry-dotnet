using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Listener;
using Sentry.Extensibility;

namespace Sentry.Quartz;

internal sealed partial class SentryCronJobListener : JobListenerSupport
{
    private readonly ILogger<SentryCronJobListener> _logger;
    private readonly SentryCronJobOptions _options;
    private readonly ConcurrentDictionary<string, SentryId> _fireInstanceId = new();
    private readonly ConcurrentDictionary<Type, SentryCronInformation> _sentryCronInformation = [];
    private readonly IHub _hub;

    public SentryCronJobListener(ILogger<SentryCronJobListener> logger, IOptions<SentryCronJobOptions> options, IHub? hub = null)
    {
        _hub = hub ?? HubAdapter.Instance;
        _logger = logger;
        _options = options.Value;
    }

    public override string Name { get; } = "Sentry Job Listener";

    public override Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var jobType = context.JobInstance.GetType();
        _sentryCronInformation.TryAdd(jobType, new SentryCronInformation(context.JobInstance));

        if (_sentryCronInformation.TryGetValue(jobType, out var info) && info.ShouldWriteStatusToSentry)
        {
            var sentryId = _hub.CaptureCheckIn(info.MonitorSlug, CheckInStatus.InProgress, configureMonitorOptions: options =>
            {
                if (_options.EnableUpsertCronMonitor && context.Trigger is ICronTrigger cronTrigger)
                {
                    UpsertCronMonitor(cronTrigger, info, options);
                }
            });

            _fireInstanceId.TryAdd(context.FireInstanceId, sentryId);
        }

        return Task.CompletedTask;
    }

    public override Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        var jobType = context.JobInstance.GetType();

        if (_sentryCronInformation.TryGetValue(jobType, out var info) && info.ShouldWriteStatusToSentry)
        {
            var status = jobException is not null ? CheckInStatus.Error : CheckInStatus.Ok;
            _fireInstanceId.TryRemove(context.FireInstanceId, out var checkInId);

            _hub.CaptureCheckIn(info.MonitorSlug, status, checkInId);
        }

        return Task.CompletedTask;
    }

    private void UpsertCronMonitor(ICronTrigger cronTrigger, SentryCronInformation information, SentryMonitorOptions options)
    {
        if (string.IsNullOrWhiteSpace(cronTrigger.CronExpressionString))
        {
            return;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(cronTrigger.TimeZone.Id, RegionInfo.CurrentRegion.TwoLetterISORegionName, out string? iana))
        {
            options.TimeZone = iana;
        }
        else
        {
            options.TimeZone = cronTrigger.TimeZone.Id;
        }

        string monitorSlug = information.MonitorSlug;
        string cron = cronTrigger.CronExpressionString.Replace("?", "*", StringComparison.OrdinalIgnoreCase);
        var cronParts = cron.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        if (cronParts.Length is 6 or 7)
        {
            if(!cronParts[0].Equals("0", StringComparison.OrdinalIgnoreCase))
            {
                if (!information.WarningShownForSecondsParameterIssue)
                {
                    information.WarningShownForSecondsParameterIssue = true;
                    LogUpdateCronMonitorGranularityWarning(monitorSlug, cron);
                }
            }
            cronParts = cronParts[1..6];
        }

        try
        {
            string crontab = string.Join(" ", cronParts);
            options.Interval(crontab);
        }
        catch (ArgumentException ex)
        {
            LogUpdateCronMonitorError(ex, monitorSlug, cron);
        }
    }

    [LoggerMessage(LogLevel.Warning, """Sentry Cron Monitor supports a minimum granularity of minutes. But for job {MonitorSlug} "{CronExpression}" was provided. The first field (seconds) will be ignored""")]
    private partial void LogUpdateCronMonitorGranularityWarning(string monitorSlug, string cronExpression);

    [LoggerMessage(LogLevel.Error, """Sentry Cron Monitor update failed for job {MonitorSlug}. Cron expression "{CronExpression}" is invalid""")]
    private partial void LogUpdateCronMonitorError(Exception ex, string monitorSlug, string cronExpression);
}
