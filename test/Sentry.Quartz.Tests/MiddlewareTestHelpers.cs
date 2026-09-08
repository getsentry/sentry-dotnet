using Quartz;

namespace Sentry.Quartz.Tests;

/// <summary>
/// Shared helpers for building the Quartz middleware pipeline types (<see cref="IJobExecutionContext"/>,
/// <see cref="ICronTrigger"/>, <see cref="JobExecutionDelegate"/>) that the Sentry.Quartz middlewares operate on,
/// without needing a real Quartz scheduler.
/// </summary>
internal static class MiddlewareTestHelpers
{
    public static IJobExecutionContext CreateContext(IJob job, ITrigger? trigger = null, IJobDetail? jobDetail = null)
    {
        trigger ??= Substitute.For<ITrigger>();
        jobDetail ??= Substitute.For<IJobDetail>();
        jobDetail.JobDataMap.Returns(new JobDataMap());

        var context = Substitute.For<IJobExecutionContext>();
        context.JobInstance.Returns(job);
        context.Trigger.Returns(trigger);
        context.JobDetail.Returns(jobDetail);
        return context;
    }

    public static ICronTrigger CreateCronTrigger(string cronExpressionString = "0 0 12 * * ?", TimeZoneInfo? timeZone = null)
    {
        var trigger = Substitute.For<ICronTrigger>();
        trigger.CronExpressionString.Returns(cronExpressionString);
        trigger.TimeZone.Returns(timeZone ?? TimeZoneInfo.Utc);
        return trigger;
    }

    /// <summary>
    /// A <see cref="JobExecutionDelegate"/> that either completes successfully, or throws/faults with the given
    /// exception, mimicking the next delegate in the Quartz job execution middleware pipeline.
    /// </summary>
    public static JobExecutionDelegate Next(Exception? throwException = null) =>
        (_, _) => throwException is null ? default : ValueTask.FromException(throwException);
}

internal sealed class PlainJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default;
}

[SentryCronMonitorSlug]
internal sealed class MonitoredJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default;
}

[SentryCronMonitorSlug("custom-slug")]
internal sealed class CustomSlugJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) => default;
}
