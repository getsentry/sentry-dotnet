namespace Sentry.Quartz.Tests;

public class JobMonitorQuartz4Tests
{
    [Fact]
    public async Task StartAsync_OtherJobTypeCantBeResolved_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var context = MiddlewareTestHelpers.CreateContext<MonitoredJob>();
        var removedJob = JobBuilder.Create().OfType(new JobType("Removed.Job, Removed")).WithIdentity("Removed").Build();
        var removedJobTrigger = TriggerBuilder.Create().ForJob(removedJob).WithCronSchedule("0 0 6 * * ?").Build();
        context.Scheduler.ReturnsTriggers([(context.JobDetail, context.Trigger), (removedJob, removedJobTrigger)]);

        // Act
        await new JobMonitor(new SentryQuartzOptions(), hub).StartAsync(context, CancellationToken.None);

        // Assert
        hub.ReceivedConfigureMonitorOptions("cleanup").Should().NotBeNull();
    }
}
