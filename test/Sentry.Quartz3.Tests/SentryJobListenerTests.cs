using System.Collections.Specialized;
using Quartz.Impl;

namespace Sentry.Quartz.Tests;

public class SentryJobListenerTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly SentryId _checkInId = SentryId.Create();
    private readonly TimeSpan _jobRunTime = TimeSpan.FromSeconds(3);

    public SentryJobListenerTests()
    {
        _hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(_checkInId);
    }

    private SentryJobListener GetSut() => new(new SentryQuartzOptions(), _hub);

    private IJobExecutionContext GetContext<TJob>() where TJob : IJob
    {
        var jobDetail = JobBuilder.Create<TJob>().WithIdentity("Cleanup").Build();
        var trigger = TriggerBuilder.Create().ForJob(jobDetail).WithSimpleSchedule(s => s.WithIntervalInHours(1).RepeatForever()).Build();
        var scheduler = Substitute.For<IScheduler>();
        scheduler.ReturnsTriggers(jobDetail.Key, [trigger]);

        var context = Substitute.For<IJobExecutionContext>();
        context.JobDetail.Returns(jobDetail);
        context.Trigger.Returns(trigger);
        context.Scheduler.Returns(scheduler);
        context.MergedJobDataMap.Returns(new JobDataMap());
        context.JobRunTime.Returns(_jobRunTime);
        return context;
    }

    [Theory]
    [InlineData(false, CheckInStatus.Ok)]
    [InlineData(true, CheckInStatus.Error)]
    public async Task JobWasExecuted_AfterJobToBeExecuted_CapturesCheckInWithDuration(bool failed, CheckInStatus expected)
    {
        // Arrange
        var sut = GetSut();
        var context = GetContext<MonitoredJob>();
        await sut.JobToBeExecuted(context);

        // Act
        await sut.JobWasExecuted(context, failed ? new JobExecutionException("failed") : null);
        await sut.JobWasExecuted(context, null);

        // Assert
        _hub.ReceivedConfigureMonitorOptions("cleanup").Should().NotBeNull();
        _hub.Received(1).CaptureCheckIn("cleanup", expected, _checkInId, _jobRunTime);
        _hub.ReceivedWithAnyArgs(2).CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task JobWasExecuted_JobWithoutAttribute_CapturesNothing()
    {
        // Arrange
        var sut = GetSut();
        var context = GetContext<NoOpJob>();
        await sut.JobToBeExecuted(context);

        // Act
        await sut.JobWasExecuted(context, null);

        // Assert
        _hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Theory]
    [InlineData("3.6.0.0", false)]
    [InlineData("3.15.1.0", false)]
    [InlineData("4.0.0.0", true)]
    [InlineData(null, false)]
    public void IsUnsupportedQuartzVersion_Version_ReturnsWhetherMajorIsAtLeastFour(string? version, bool expected)
    {
        SentryJobListener.IsUnsupportedQuartzVersion(version is null ? null : Version.Parse(version)).Should().Be(expected);
    }

    [Fact]
    public async Task Scheduler_CronTrigger_CapturesCheckInsWithCrontab()
    {
        // Arrange
        var completed = new TaskCompletionSource<CheckInStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(call =>
        {
            if (call.ArgAt<CheckInStatus>(1) != CheckInStatus.InProgress)
            {
                completed.TrySetResult(call.ArgAt<CheckInStatus>(1));
            }

            return _checkInId;
        });
        var properties = new NameValueCollection { ["quartz.scheduler.instanceName"] = Guid.NewGuid().ToString() };
        var scheduler = await new StdSchedulerFactory(properties).GetScheduler();
        scheduler.ListenerManager.AddJobListener(GetSut());
        var second = DateTimeOffset.UtcNow.AddSeconds(3).Second;
        await scheduler.ScheduleJob(
            JobBuilder.Create<MonitoredJob>().WithIdentity("Cleanup").Build(),
            TriggerBuilder.Create().WithCronSchedule($"{second} * * * * ?", s => s.InTimeZone(TimeZoneInfo.Utc)).Build());

        // Act
        await scheduler.Start();
        try
        {
            (await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(70)))).Should().BeSameAs(completed.Task);
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }

        // Assert
        (await completed.Task).Should().Be(CheckInStatus.Ok);
        var monitorConfig = MonitorConfigJson.Render(_hub.ReceivedConfigureMonitorOptions("cleanup")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("* * * * *");
    }
}
