namespace Sentry.Quartz.Tests;

// The Quartz 4 side of the test helpers whose APIs differ between Quartz 3 and 4
internal static class QuartzTestApi
{
    public static TriggerBuilder<TJob> WithCalendar<TJob>(this TriggerBuilder<TJob> builder, string calendarName) where TJob : IJob =>
        builder.WithCalendarName(calendarName);

    public static void ReturnsTriggers(this IScheduler scheduler, JobKey jobKey, IReadOnlyList<ITrigger> triggers)
    {
        var headers = triggers
            .Select(t => new TriggerHeader(t.Key, jobKey, null, "CRON", TriggerState.Normal, t.StartTimeUtc,
                null, null, null, null, t.Priority, null, null, 0))
            .ToList();
        scheduler.QueryTriggers(Arg.Is<TriggerQuery>(q => q.Job == jobKey), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TriggerHeader>(headers, HasMore: false));
    }

    public static void TriggerLookupThrows(this IScheduler scheduler, Exception exception) =>
        scheduler.QueryTriggers(Arg.Any<TriggerQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException<PagedResult<TriggerHeader>>(exception));

    public static void DidNotLookUpTriggers(this IScheduler scheduler) =>
        _ = scheduler.DidNotReceiveWithAnyArgs().QueryTriggers(default!, default);
}

internal class NoOpJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default) => default;
}

[SentryCronMonitorSlug]
internal class MonitoredJob : NoOpJob;

[SentryCronMonitorSlug("custom-slug")]
internal class CustomSlugJob : NoOpJob;

[SentryCronMonitorSlug(SendMonitorConfig = false)]
internal class NoMonitorConfigJob : NoOpJob;

[SentryCronMonitorSlug]
internal class FailingJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Job failed");
}
