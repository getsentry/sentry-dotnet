namespace Sentry.Hangfire.Tests;

public class HangfireTests : IClassFixture<HangfireFixture>
{
    private readonly HangfireFixture _fixture;

    public HangfireTests(HangfireFixture hangfireFixture)
    {
        _fixture = hangfireFixture;
    }

    [Fact]
    public async void ExecuteJobWithAttribute_CapturesCheckInInProgressAndOk()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<SentryCheckIn>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithAttribute());

        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job" &&
            checkIn.Status == CheckInStatus.InProgress));
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job" &&
            checkIn.Id.Equals(sentryId) &&
            checkIn.Status == CheckInStatus.Ok));
    }

    [Fact]
    public async void ExecuteJobWithException_CapturesCheckInInProgressAndError()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<SentryCheckIn>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithException());

        await Task.Delay(1000);

        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job-with-exception" &&
            checkIn.Status == CheckInStatus.InProgress));
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == "test-job-with-exception" &&
            checkIn.Id.Equals(sentryId) &&
            checkIn.Status == CheckInStatus.Error));
    }

    [Fact]
    public async void ExecuteJobWithoutAttribute_DoesNotCapturesCheckInButLogs()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<SentryCheckIn>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithoutAttribute());

        _fixture.Hub.DidNotReceive().CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn => checkIn.Id.Equals(sentryId)));
        _fixture.Logger.Received(1).Log(SentryLevel.Warning, Arg.Is<string>(message => message.Contains("Skipping creating a check-in for")), null, Arg.Any<Type>(), Arg.Any<MethodInfo>());
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
