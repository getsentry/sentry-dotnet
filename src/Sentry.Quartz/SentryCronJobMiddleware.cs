using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz;

internal sealed partial class SentryCronJobMiddleware : IJobExecutionMiddleware
{
    private readonly SentryCronJobOptions _options;
    private readonly ConcurrentDictionary<Type, SentryCronInformation> _sentryCronInformation = [];
    private readonly IHub _hub;
    private readonly ILogger<SentryCronJobMiddleware> _logger;

    public SentryCronJobMiddleware(IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobMiddleware> logger)
    {
        _hub = hub;
        _logger = logger;
        _options = options.Value;
    }

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        var jobType = context.JobInstance.GetType();
        var info = _sentryCronInformation.GetOrAdd(jobType, _ => new SentryCronInformation(context.JobInstance));

        var sentryId = StartQuartz(context, info);
        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            CompleteCheckIn(info, sentryId, CheckInStatus.Ok);
        }
        catch
        {
            CompleteCheckIn(info, sentryId, CheckInStatus.Error);
            throw;
        }
    }

    private SentryId? StartQuartz(IJobExecutionContext context, SentryCronInformation info)
    {
        if (info.ShouldWriteStatusToSentry)
        {
            return _hub.CaptureCheckIn(info.MonitorSlug, CheckInStatus.InProgress, configureMonitorOptions: options =>
            {
                if (_options.EnableUpsertCronMonitor && context.Trigger is ICronTrigger cronTrigger)
                {
                    UpsertCronMonitor(cronTrigger, info, options);
                }
            });
        }

        return null;
    }

    private void CompleteCheckIn(SentryCronInformation info, SentryId? sentryId, CheckInStatus status)
    {
        if (sentryId is not null)
        {
            _hub.CaptureCheckIn(info.MonitorSlug, status, sentryId);
        }
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
        var cronSpan = cron.Split(" ", StringSplitOptions.RemoveEmptyEntries)
#if NET9_0_OR_GREATER
            .AsSpan()
#endif
            ;

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
