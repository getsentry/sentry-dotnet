namespace Sentry.Quartz.Tests;

public class SentryCronJobMiddlewareTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly SentryId _checkInId = SentryId.Create();

    public SentryCronJobMiddlewareTests()
    {
        _hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(_checkInId);
    }

    private SentryCronJobMiddleware GetSut() => new(new JobMonitor(new SentryQuartzOptions(), _hub));

    [Fact]
    public async Task Invoke_JobWithoutAttribute_RunsJobWithoutCheckIns()
    {
        // Arrange
        var ran = false;

        // Act
        await GetSut().Invoke(MiddlewareTestHelpers.CreateContext<NoOpJob>(), (_, _) =>
        {
            ran = true;
            return default;
        }, CancellationToken.None);

        // Assert
        ran.Should().BeTrue();
        _hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task Invoke_JobSucceeds_CapturesInProgressThenOkCheckIn()
    {
        // Act
        await GetSut().Invoke(MiddlewareTestHelpers.CreateContext<MonitoredJob>(), MiddlewareTestHelpers.Next(), CancellationToken.None);

        // Assert
        _hub.ReceivedConfigureMonitorOptions("cleanup").Should().NotBeNull();
        _hub.Received(1).CaptureCheckIn("cleanup", CheckInStatus.Ok, _checkInId, Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task Invoke_JobThrows_CapturesErrorCheckInAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Job failed");

        // Act
        var act = () => GetSut().Invoke(MiddlewareTestHelpers.CreateContext<MonitoredJob>(), MiddlewareTestHelpers.Next(exception), CancellationToken.None).AsTask();

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        _hub.Received(1).CaptureCheckIn("cleanup", CheckInStatus.Error, _checkInId, Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task Invoke_JobCancelled_CapturesErrorCheckIn()
    {
        // Arrange
        var cancelled = new CancellationToken(canceled: true);

        // Act
        await GetSut().Invoke(MiddlewareTestHelpers.CreateContext<MonitoredJob>(cancelled), MiddlewareTestHelpers.Next(), cancelled);

        // Assert
        _hub.Received(1).CaptureCheckIn("cleanup", CheckInStatus.Error, _checkInId, Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task Invoke_StartingCheckInThrows_RunsJob()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.CaptureCheckIn(default!, default, default, default, default, default)
            .ThrowsForAnyArgs(new InvalidOperationException("Hub failed"));
        var sut = new SentryCronJobMiddleware(new JobMonitor(new SentryQuartzOptions(), hub));
        var ran = false;

        // Act
        await sut.Invoke(MiddlewareTestHelpers.CreateContext<MonitoredJob>(), (_, _) =>
        {
            ran = true;
            return default;
        }, CancellationToken.None);

        // Assert
        ran.Should().BeTrue();
    }
}
