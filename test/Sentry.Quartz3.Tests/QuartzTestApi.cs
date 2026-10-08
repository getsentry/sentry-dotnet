namespace Sentry.Quartz.Tests;

// The Quartz 3 side of the test helpers whose APIs differ between Quartz 3 and 4
internal static class QuartzTestApi
{
    public static TriggerBuilder WithCalendar(this TriggerBuilder builder, string calendarName) =>
        builder.ModifiedByCalendar(calendarName);

    public static void ReturnsTriggers(this IScheduler scheduler, JobKey jobKey, IReadOnlyList<ITrigger> triggers) =>
        scheduler.GetTriggersOfJob(jobKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<ITrigger>>(triggers.ToList()));

    public static void TriggerLookupThrows(this IScheduler scheduler, Exception exception) =>
        scheduler.GetTriggersOfJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).ThrowsAsync(exception);

    public static void DidNotLookUpTriggers(this IScheduler scheduler) =>
        _ = scheduler.DidNotReceiveWithAnyArgs().GetTriggersOfJob(default!, default);
}

internal class NoOpJob : IJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
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
    public Task Execute(IJobExecutionContext context) => throw new InvalidOperationException("Job failed");
}
