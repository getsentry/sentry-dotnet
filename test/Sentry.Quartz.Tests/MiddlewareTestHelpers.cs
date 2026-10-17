namespace Sentry.Quartz.Tests;

internal static class MiddlewareTestHelpers
{
    public static IJobExecutionContext CreateContext<TJob>(CancellationToken cancellationToken = default) where TJob : IJob
    {
        var jobDetail = JobBuilder.Create<TJob>().WithIdentity("Cleanup").Build();
        var trigger = TriggerBuilder.Create()
            .ForJob(jobDetail)
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();
        var scheduler = Substitute.For<IScheduler>();
        scheduler.ReturnsTriggers(jobDetail.Key, [trigger]);

        var context = Substitute.For<IJobExecutionContext>();
        context.JobDetail.Returns(jobDetail);
        context.Trigger.Returns(trigger);
        context.Scheduler.Returns(scheduler);
        context.MergedJobDataMap.Returns(new JobDataMap());
        context.CancellationToken.Returns(cancellationToken);
        return context;
    }

    // The rest of the pipeline, which completes or throws the given exception
    public static JobExecutionDelegate Next(Exception? throwException = null) =>
        (_, _) => throwException is null ? default : ValueTask.FromException(throwException);
}
