using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz;

internal sealed partial class SentryCronJobListener : IJobListener
{
    private readonly SentryCronJobOptions _options;
    private readonly ConcurrentDictionary<string, SentryId> _fireInstanceId = new();
    private readonly ConcurrentDictionary<Type, SentryCronInformation> _sentryCronInformation = [];
    private readonly IHub _hub;
    private readonly ILogger<SentryCronJobListener> _logger;

    public SentryCronJobListener(IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobListener> logger)
    {
        _hub = hub;
        _logger = logger;
        _options = options.Value;
    }

    public string Name { get; } = "Sentry Job Listener";

    public ValueTask JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var jobType = context.JobInstance.GetType();
        var info = _sentryCronInformation.GetOrAdd(jobType, _ => new SentryCronInformation(context.JobInstance));

        if (info.ShouldWriteStatusToSentry)
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

        return ValueTask.CompletedTask;
    }

    public ValueTask JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        var jobType = context.JobInstance.GetType();

        if (_sentryCronInformation.TryGetValue(jobType, out var info) && info.ShouldWriteStatusToSentry)
        {
            var status = jobException is not null ? CheckInStatus.Error : CheckInStatus.Ok;
            _fireInstanceId.TryRemove(context.FireInstanceId, out var checkInId);

            _hub.CaptureCheckIn(info.MonitorSlug, status, checkInId);
        }

        return ValueTask.CompletedTask;
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
        var cronSpan = cron.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        if (cronSpan.Length is 6 or 7)
        {
            if (!cronSpan[0].Equals("0", StringComparison.OrdinalIgnoreCase))
            {
                if (!information.WarningShownForSecondsParameterIssue)
                {
                    information.WarningShownForSecondsParameterIssue = true;
                    LogGranularityWarning(monitorSlug, cron);
                }
            }
            cronSpan = cronSpan[1..6];
        }

        try
        {
            string crontab = string.Join(" ", cronSpan);
            options.Interval(crontab);
        }
        catch (ArgumentException ex)
        {
            LogApiException(ex, monitorSlug, cron);
        }
    }

    [LoggerMessage(1, LogLevel.Warning, "Sentry Cron Monitor supports a minimum granularity of minutes. But for job {MonitorSlug} \"{Cron}\" was provided. The first field (seconds) will be ignored")]
    private partial void LogGranularityWarning(string monitorSlug, string cron);

    [LoggerMessage(2, LogLevel.Error, "Sentry Cron Monitor update failed for job {MonitorSlug}. Cron expression \"{Cron}\" is invalid")]
    private partial void LogApiException(Exception exception, string monitorSlug, string cron);
}
