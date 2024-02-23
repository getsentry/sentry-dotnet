using Hangfire;
using Hangfire.MemoryStorage;

namespace Sentry.Hangfire.Tests;

public class HangfireTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public IDiagnosticLogger Logger { get; }
        public BackgroundJobServer Server { get; }

        public Fixture()
        {
            Logger = Substitute.For<IDiagnosticLogger>();
            Logger.IsEnabled(SentryLevel.Warning).Returns(true);
            Hub.IsEnabled.Returns(true);

            GlobalConfiguration.Configuration
                .UseMemoryStorage()
                .UseSentry(Hub, Logger);
            Server = new BackgroundJobServer();
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async void ExecuteJobWithAttribute_CapturesCheckInInProgressAndOk()
    {
        BackgroundJob.Enqueue<TestJob>(job => job.ExecuteJobWithAttribute());

        await Task.Delay(100);

        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job" &&
            checkIn.Status == CheckInStatus.InProgress));
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job" &&
            checkIn.Status == CheckInStatus.Ok));
    }

    [Fact]
    public async void ExecuteJobWithException_CapturesCheckInInProgressAndError()
    {
        BackgroundJob.Enqueue<TestJob>(job => job.ExecuteJobWithException());

        await Task.Delay(100);

        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job-with-exception" &&
            checkIn.Status == CheckInStatus.InProgress));
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job-with-exception" &&
            checkIn.Status == CheckInStatus.Error));
    }

    [Fact]
    public async void ExecuteJobWithoutAttribute_DoesNotCapturesCheckInButLogs()
    {
        BackgroundJob.Enqueue<TestJob>(job => job.ExecuteJobWithoutAttribute());

        await Task.Delay(100);

        _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(Arg.Any<SentryCheckIn>());
        _fixture.Logger.Received(1).Log(SentryLevel.Warning, Arg.Any<string>(), null, Arg.Any<string>());
    }

}


public class TestJob
{
    [SentryMonitorSlug("test-job")]
    public void ExecuteJobWithAttribute()
    { }

    [SentryMonitorSlug("test-job-with-exception")]
    public void ExecuteJobWithException()
    {
        throw new Exception();
    }

    public void ExecuteJobWithoutAttribute()
    { }
}
