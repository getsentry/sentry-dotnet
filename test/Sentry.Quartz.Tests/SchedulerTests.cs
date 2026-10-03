using System.Collections.Specialized;
using Quartz.Impl;

namespace Sentry.Quartz.Tests;

public class SchedulerTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly SentryId _checkInId = SentryId.Create();
    private readonly TaskCompletionSource<CheckInStatus> _jobCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SchedulerTests()
    {
        _hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(call =>
        {
            var status = call.ArgAt<CheckInStatus>(1);
            if (status != CheckInStatus.InProgress)
            {
                _jobCompleted.TrySetResult(status);
            }

            return _checkInId;
        });
    }

    [Fact]
    public async Task CronTrigger_JobSucceeds_CapturesCheckInsWithCrontab()
    {
        // Arrange
        var job = JobBuilder.Create<NoOpJob>().WithIdentity("Cleanup").Build();
        var second = DateTimeOffset.UtcNow.AddSeconds(3).Second;
        var trigger = TriggerBuilder.Create()
            .WithCronSchedule($"{second} * * * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();

        // Act
        var status = await RunUntilJobCompletes(job, trigger);

        // Assert
        status.Should().Be(CheckInStatus.Ok);
        var monitorConfig = MonitorConfigJson.Render(_hub.ReceivedConfigureMonitorOptions("cleanup")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("* * * * *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("UTC");
        _hub.Received(1).CaptureCheckIn("cleanup", CheckInStatus.Ok, _checkInId, Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task SimpleTrigger_JobFails_CapturesCheckInsWithInterval()
    {
        // Arrange
        var job = JobBuilder.Create<FailingJob>().WithIdentity("Sync", "billing").Build();
        var trigger = TriggerBuilder.Create()
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithIntervalInMinutes(15).RepeatForever())
            .Build();

        // Act
        var status = await RunUntilJobCompletes(job, trigger);

        // Assert
        status.Should().Be(CheckInStatus.Error);
        var monitorConfig = MonitorConfigJson.Render(_hub.ReceivedConfigureMonitorOptions("billing-sync")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetInt32().Should().Be(15);
        monitorConfig.GetProperty("schedule").GetProperty("unit").GetString().Should().Be("minute");
    }

    private async Task<CheckInStatus> RunUntilJobCompletes(IJobDetail job, ITrigger trigger)
    {
        var properties = new NameValueCollection { ["quartz.scheduler.instanceName"] = Guid.NewGuid().ToString() };
        var scheduler = await new StdSchedulerFactory(properties).GetScheduler();
        scheduler.ListenerManager.AddJobListener(new SentryJobListener(new SentryQuartzOptions(), _hub));
        await scheduler.ScheduleJob(job, trigger);
        await scheduler.Start();
        try
        {
            var completed = await Task.WhenAny(_jobCompleted.Task, Task.Delay(TimeSpan.FromSeconds(70)));
            completed.Should().BeSameAs(_jobCompleted.Task, "the job should run");
            return await _jobCompleted.Task;
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }
    }

    private class FailingJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => throw new InvalidOperationException("Job failed");
    }
}
