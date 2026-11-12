using System.Collections.Specialized;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        _hub.Received(1).CaptureCheckIn("cleanup", CheckInStatus.Ok, _hub.ReceivedInProgressCheckInId("cleanup"), Arg.Any<TimeSpan?>());
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
    public async Task AddSentry_MonitorAllJobs_CapturesCheckInsForJobWithoutAttribute()
    {
        // Arrange
        var second = DateTimeOffset.UtcNow.AddSeconds(3).Second;
        var options = new SentryQuartzOptions { MonitorAllJobs = true };

        // Act
        var status = await RunUntilCheckInCompletes<NoOpJob>("Nightly Report", null, trigger => trigger
            .WithCronSchedule($"{second} * * * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc)), options);

        // Assert
        status.Should().Be(CheckInStatus.Ok);
        var monitorConfig = MonitorConfigJson.Render(_hub.ReceivedConfigureMonitorOptions("nightly-report")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("* * * * *");
    }

    [Fact]
    public async Task AddSentry_MonitoredJob_CapturesCheckInsInJobScope()
    {
        // Arrange
        var jobScope = Substitute.For<IDisposable>();
        _hub.PushScope().Returns(jobScope);

        // Act
        await RunUntilCheckInCompletes<MonitoredJob>("Cleanup", null, trigger => trigger
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(15)).RepeatForever()));

        // Assert
        Received.InOrder(() =>
        {
            _hub.PushScope();
            _hub.CaptureCheckIn("cleanup", CheckInStatus.InProgress, Arg.Any<SentryId?>(), Arg.Any<TimeSpan?>(), Arg.Any<Scope?>(), Arg.Any<Action<SentryMonitorOptions>?>());
            _hub.CaptureCheckIn("cleanup", CheckInStatus.Ok, Arg.Any<SentryId?>(), Arg.Any<TimeSpan?>(), Arg.Any<Scope?>(), Arg.Any<Action<SentryMonitorOptions>?>());
            jobScope.Dispose();
        });
    }

    [Fact]
    public async Task AddSentry_JobFailsWithLoggingIntegration_CapturesExceptionOnce()
    {
        // Arrange
        // The logging integration sends to the global hub
        using var _ = SentrySdk.UseHub(_hub);

        // Act
        await RunUntilCheckInCompletes<FailingJob>("Sync", "billing", trigger => trigger.StartNow(), configureServices: services => services
            .AddLogging(logging => logging.AddSentry()));

        // Assert
        AssertJobExceptionCapturedOnce();
    }

    [Fact]
    public async Task AddSentry_JobFailsWithAspNetCoreUseSentry_CapturesExceptionOnce()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(web => web.UseSentry().Configure(_ => { }))
            .ConfigureServices(services =>
            {
                services.AddSingleton(_hub);
                AddScheduler<FailingJob>(services, "Sync", "billing", trigger => trigger.StartNow());
            })
            .Build();
        var scheduler = await host.Services.GetRequiredService<ISchedulerFactory>().GetScheduler();

        // Act
        await RunUntilCheckInCompletes(scheduler);

        // Assert
        AssertJobExceptionCapturedOnce();
    }

    [Fact]
    public void AddSentry_UnrelatedOptions_StillResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<UnrelatedOptions>(options => options.Value = 1);
        services.AddQuartz(quartz => quartz.AddSentry(new SentryQuartzOptions(), _hub));

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IOptions<UnrelatedOptions>>().Value.Value.Should().Be(1);
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
        Action<ITriggerConfigurator<TJob>> configureTrigger,
        SentryQuartzOptions? options = null,
        Action<IServiceCollection>? configureServices = null) where TJob : IJob =>
        await RunUntilCheckInCompletes(await BuildScheduler<TJob>(name, group, configureTrigger, options, configureServices));

    private async Task<CheckInStatus> RunUntilCheckInCompletes(IScheduler scheduler)
    {
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
        Action<ITriggerConfigurator<TJob>> configureTrigger,
        SentryQuartzOptions? options = null,
        Action<IServiceCollection>? configureServices = null) where TJob : IJob
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        AddScheduler(services, name, group, configureTrigger, options);

        var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
    }

    private void AddScheduler<TJob>(
        IServiceCollection services,
        string name,
        string? group,
        Action<ITriggerConfigurator<TJob>> configureTrigger,
        SentryQuartzOptions? options = null) where TJob : IJob
    {
        var properties = new NameValueCollection { ["quartz.scheduler.instanceName"] = Guid.NewGuid().ToString() };
        services.AddQuartz(properties, quartz =>
        {
            quartz.UseInMemoryStore();
            quartz.AddSentry(options ?? new SentryQuartzOptions(), _hub);

            var jobKey = new JobKey(name, group ?? JobKey.DefaultGroup);
            quartz.AddJob<TJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger<TJob>(trigger => configureTrigger(trigger.ForJob(jobKey)));
        });
    }

    // Quartz also logs the job's own exception, which Sentry drops as the same exception instance
    private void AssertJobExceptionCapturedOnce()
    {
        var exceptions = _hub.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IHub.CaptureEvent))
            .Select(call => ((SentryEvent)call.GetArguments()[0]!).Exception)
            .ToList();
        exceptions.Should().NotBeEmpty();
        exceptions.Should().AllSatisfy(exception => exception.Should().BeOfType<InvalidOperationException>()
            .And.BeSameAs(exceptions[0]));
    }

    private sealed class UnrelatedOptions
    {
        public int Value { get; set; }
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
