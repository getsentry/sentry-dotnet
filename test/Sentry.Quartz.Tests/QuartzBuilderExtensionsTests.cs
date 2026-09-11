using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz.Tests;

/// <summary>
/// End-to-end tests that run a real, in-memory Quartz scheduler wired up through <see cref="IQuartzBuilderExtensions"/>,
/// to verify the middlewares are registered and invoked correctly - as opposed to the other test classes in this
/// project, which exercise each middleware directly.
/// </summary>
public class QuartzBuilderExtensionsTests
{
    [Fact]
    public async Task AddSentryCronJobs_JobWithMonitorSlug_CapturesCheckInsAroundExecution()
    {
        var hub = Substitute.For<IHub>();
        var startId = SentryId.Create();
        hub.CaptureCheckIn(nameof(TrackingJob), CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>())
            .Returns(startId);

        using var jobRan = new SemaphoreSlim(0, 1);
        TrackingJob.Signal = jobRan;

        var services = new ServiceCollection();
        services.AddQuartz(quartz =>
        {
            quartz.UseInMemoryStore();
            quartz.AddSentryCronJobs(Options.Create(new SentryCronJobOptions()), hub, NullLogger<SentryCronJobMiddleware>.Instance);

            var jobKey = new JobKey(nameof(TrackingJob));
            quartz.AddJob<TrackingJob>(opts => opts.WithIdentity(jobKey));
            quartz.AddTrigger<TrackingJob>(opts => opts.ForJob(jobKey).WithCronSchedule("* * * * * ?"));
        });

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        await scheduler.Start();
        try
        {
            var jobExecuted = await jobRan.WaitAsync(TimeSpan.FromSeconds(15));
            jobExecuted.Should().BeTrue("the job should have run within the timeout");
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }

        hub.Received().CaptureCheckIn(nameof(TrackingJob), CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>());
        hub.Received().CaptureCheckIn(nameof(TrackingJob), CheckInStatus.Ok, startId);
    }

    [Fact]
    public async Task AddSentryCronJobs_JobWithoutMonitorSlug_NeverCapturesCheckIns()
    {
        var hub = Substitute.For<IHub>();

        using var jobRan = new SemaphoreSlim(0, 1);
        UnmonitoredTrackingJob.Signal = jobRan;

        var services = new ServiceCollection();
        services.AddQuartz(quartz =>
        {
            quartz.UseInMemoryStore();
            quartz.AddSentryCronJobs(Options.Create(new SentryCronJobOptions()), hub, NullLogger<SentryCronJobMiddleware>.Instance);

            var jobKey = new JobKey(nameof(UnmonitoredTrackingJob));
            quartz.AddJob<UnmonitoredTrackingJob>(opts => opts.WithIdentity(jobKey));
            quartz.AddTrigger<UnmonitoredTrackingJob>(opts => opts.ForJob(jobKey).WithCronSchedule("* * * * * ?"));
        });

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        await scheduler.Start();
        try
        {
            var jobExecuted = await jobRan.WaitAsync(TimeSpan.FromSeconds(15));
            jobExecuted.Should().BeTrue("the job should have run within the timeout");
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }

        hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [SentryCronMonitorSlug]
    private sealed class TrackingJob : IJob
    {
        public static SemaphoreSlim? Signal { get; set; }

        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            Signal?.Release();
            return default;
        }
    }

    private sealed class UnmonitoredTrackingJob : IJob
    {
        public static SemaphoreSlim? Signal { get; set; }

        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            Signal?.Release();
            return default;
        }
    }
}
