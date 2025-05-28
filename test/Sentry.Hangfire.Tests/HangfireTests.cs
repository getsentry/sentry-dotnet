namespace Sentry.Hangfire.Tests;

public class HangfireTests : IClassFixture<HangfireFixture>
{
    private readonly HangfireFixture _fixture;

    public HangfireTests(HangfireFixture hangfireFixture)
    {
        _fixture = hangfireFixture;
    }

    [Fact]
    public async Task ExecuteJobWithAttribute_CapturesCheckInInProgressAndOkWithDuration()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithAttribute());

        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.InProgress),
            Arg.Any<SentryId?>());
        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.Ok),
            Arg.Is<SentryId?>(id => id == sentryId),
            Arg.Is<TimeSpan?>(duration => duration != null), Arg.Any<Scope>());
    }

    [Fact]
    public async Task ExecuteJobWithException_CapturesCheckInInProgressAndErrorWithDuration()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithException());

        await Task.Delay(1000);

        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job-with-exception"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.InProgress),
            Arg.Any<SentryId?>());
        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job-with-exception"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.Error),
            Arg.Is<SentryId?>(id => id == sentryId),
            Arg.Is<TimeSpan?>(duration => duration != null), Arg.Any<Scope>());
    }

    [Fact]
    public async Task ExecuteJobWithoutAttribute_DoesNotCapturesCheckInButLogs()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithoutAttribute());

        _fixture.Hub.DidNotReceive().CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), Arg.Is<SentryId>(id => id == sentryId));
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, Arg.Is<string>(message => message.Contains("Skipping creating a check-in for")), null, Arg.Any<Type>(), Arg.Any<MethodInfo>());
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
