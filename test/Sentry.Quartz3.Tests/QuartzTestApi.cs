using Quartz.Impl.Matchers;

namespace Sentry.Quartz.Tests;

// The Quartz 3 side of the test helpers whose APIs differ between Quartz 3 and 4
internal static class QuartzTestApi
{
    public static TriggerBuilder WithCalendar(this TriggerBuilder builder, string calendarName) =>
        builder.ModifiedByCalendar(calendarName);

    public static void ReturnsTriggers(this IScheduler scheduler, IReadOnlyList<(IJobDetail Job, ITrigger Trigger)> triggers)
    {
        var jobs = triggers.Select(t => t.Job).GroupBy(job => job.Key).Select(group => group.First()).ToList();
        scheduler.GetJobKeys(Arg.Any<GroupMatcher<JobKey>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<JobKey>>(jobs.Select(job => job.Key).ToList()));
        scheduler.GetJobDetail(Arg.Any<JobKey>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(jobs.First(job => job.Key.Equals(call.Arg<JobKey>()))));
        scheduler.GetTriggersOfJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyCollection<ITrigger>>(triggers
                .Where(t => t.Job.Key.Equals(call.Arg<JobKey>())).Select(t => t.Trigger).ToList()));
    }

    public static void TriggerLookupThrows(this IScheduler scheduler, Exception exception) =>
        scheduler.GetJobKeys(Arg.Any<GroupMatcher<JobKey>>(), Arg.Any<CancellationToken>()).ThrowsAsync(exception);

    public static void DidNotLookUpTriggers(this IScheduler scheduler) =>
        _ = scheduler.DidNotReceiveWithAnyArgs().GetJobKeys(default!, default);

    public static void LookedUpTriggers(this IScheduler scheduler, int count) =>
        _ = scheduler.ReceivedWithAnyArgs(count).GetJobKeys(default!, default);
}

internal class NoOpJob : IJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
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
    public Task Execute(IJobExecutionContext context) => throw new InvalidOperationException("Job failed");
}
