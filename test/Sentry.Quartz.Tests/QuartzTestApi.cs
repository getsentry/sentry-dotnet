namespace Sentry.Quartz.Tests;

// The Quartz 4 side of the test helpers whose APIs differ between Quartz 3 and 4
internal static class QuartzTestApi
{
    public static TriggerBuilder<TJob> WithCalendar<TJob>(this TriggerBuilder<TJob> builder, string calendarName) where TJob : IJob =>
        builder.WithCalendarName(calendarName);

    public static void ReturnsTriggers(this IScheduler scheduler, IReadOnlyList<(IJobDetail Job, ITrigger Trigger)> triggers)
    {
        var headers = triggers
            .Select(t => new TriggerHeader(t.Trigger.Key, t.Job.Key, null, "CRON", TriggerState.Normal,
                t.Trigger.StartTimeUtc, null, null, null, null, t.Trigger.Priority, null, null, 0))
            .ToList();
        scheduler.QueryTriggers(Arg.Any<TriggerQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TriggerHeader>(headers, HasMore: false));
        scheduler.GetJobDetails(Arg.Any<IReadOnlyCollection<JobKey>>(), Arg.Any<CancellationToken>())
            .Returns(call => triggers.Select(t => t.Job).DistinctBy(job => job.Key)
                .Where(job => call.Arg<IReadOnlyCollection<JobKey>>().Contains(job.Key)).ToList());
        scheduler.GetTriggers(Arg.Any<IReadOnlyCollection<TriggerKey>>(), Arg.Any<CancellationToken>())
            .Returns(call => triggers.Select(t => t.Trigger)
                .Where(trigger => call.Arg<IReadOnlyCollection<TriggerKey>>().Contains(trigger.Key)).ToList());
    }

    public static void TriggerLookupThrows(this IScheduler scheduler, Exception exception) =>
        scheduler.QueryTriggers(Arg.Any<TriggerQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException<PagedResult<TriggerHeader>>(exception));

    public static void DidNotLookUpTriggers(this IScheduler scheduler) =>
        _ = scheduler.DidNotReceiveWithAnyArgs().QueryTriggers(default!, default);

    public static void LookedUpTriggers(this IScheduler scheduler, int count) =>
        _ = scheduler.ReceivedWithAnyArgs(count).QueryTriggers(default!, default);
}

internal class NoOpJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default) => default;
}

[SentryMonitorSlug]
internal class MonitoredJob : NoOpJob;

[SentryMonitorSlug("custom-slug")]
internal class CustomSlugJob : NoOpJob;

[SentryMonitorSlug(SendMonitorConfig = false)]
internal class NoMonitorConfigJob : NoOpJob;

[SentryMonitorSlug]
internal class FailingJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Job failed");
}
