using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Quartz.Tests;

// Runs a real in-memory scheduler set up through AddSentry
public class SentryQuartzBuilderExtensionsTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly SentryId _checkInId = SentryId.Create();
    private readonly TaskCompletionSource<CheckInStatus> _jobCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SentryQuartzBuilderExtensionsTests()
    {
        _hub.IsEnabled.Returns(true);
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
    public async Task AddSentry_CronTriggerJobSucceeds_CapturesCheckInsWithCrontab()
    {
        // Arrange
        var second = DateTimeOffset.UtcNow.AddSeconds(3).Second;

        // Act
        var status = await RunUntilCheckInCompletes<MonitoredJob>("Cleanup", null, trigger => trigger
            .WithCronSchedule($"{second} * * * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc)));

        // Assert
        status.Should().Be(CheckInStatus.Ok);
        var monitorConfig = MonitorConfigJson.Render(_hub.ReceivedConfigureMonitorOptions("cleanup")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("* * * * *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("UTC");
        _hub.Received(1).CaptureCheckIn("cleanup", CheckInStatus.Ok, _checkInId, Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task AddSentry_SimpleTriggerJobFails_CapturesCheckInsWithIntervalAndException()
    {
        // Act
        var status = await RunUntilCheckInCompletes<FailingJob>("Sync", "billing", trigger => trigger
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(15)).RepeatForever()));

        // Assert
        status.Should().Be(CheckInStatus.Error);
        var monitorConfig = MonitorConfigJson.Render(_hub.ReceivedConfigureMonitorOptions("billing-sync")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetInt32().Should().Be(15);
        monitorConfig.GetProperty("schedule").GetProperty("unit").GetString().Should().Be("minute");
        _hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception is InvalidOperationException));
    }

    [Fact]
    public async Task AddSentry_JobWithoutAttribute_CapturesNoCheckIns()
    {
        // Arrange
        using var jobRan = new SemaphoreSlim(0, 1);
        UnmonitoredSignallingJob.Signal = jobRan;
        var scheduler = await BuildScheduler<UnmonitoredSignallingJob>("Unmonitored", null, trigger => trigger.StartNow());

        // Act
        await scheduler.Start();
        try
        {
            var jobExecuted = await jobRan.WaitAsync(TimeSpan.FromSeconds(15));
            jobExecuted.Should().BeTrue("the job should run");
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }

        // Assert
        _hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    private async Task<CheckInStatus> RunUntilCheckInCompletes<TJob>(
        string name,
        string? group,
        Action<ITriggerConfigurator<TJob>> configureTrigger) where TJob : IJob
    {
        var scheduler = await BuildScheduler<TJob>(name, group, configureTrigger);
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

    private async Task<IScheduler> BuildScheduler<TJob>(
        string name,
        string? group,
        Action<ITriggerConfigurator<TJob>> configureTrigger) where TJob : IJob
    {
        var services = new ServiceCollection();
        var properties = new NameValueCollection { ["quartz.scheduler.instanceName"] = Guid.NewGuid().ToString() };
        services.AddQuartz(properties, quartz =>
        {
            quartz.UseInMemoryStore();
            quartz.AddSentry(new SentryQuartzOptions(), _hub);

            var jobKey = new JobKey(name, group ?? JobKey.DefaultGroup);
            quartz.AddJob<TJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger<TJob>(trigger => configureTrigger(trigger.ForJob(jobKey)));
        });

        var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
    }

    private sealed class UnmonitoredSignallingJob : IJob
    {
        public static SemaphoreSlim? Signal { get; set; }

        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            Signal?.Release();
            return default;
        }
    }
}
