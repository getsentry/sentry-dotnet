namespace Sentry.Quartz.Tests;

public class SentryScopeMiddlewareTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly Scope _scope = new();
    private readonly List<string> _events = [];

    public SentryScopeMiddlewareTests()
    {
        _hub.IsEnabled.Returns(true);
        var pushedScope = Substitute.For<IDisposable>();
        pushedScope.When(s => s.Dispose()).Do(_ => _events.Add("dispose"));
        _hub.PushScope().Returns(_ =>
        {
            _events.Add("push");
            return pushedScope;
        });
        _hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope, IJobExecutionContext>>(), Arg.Any<IJobExecutionContext>()))
            .Do(call => call.Arg<Action<Scope, IJobExecutionContext>>()(_scope, call.Arg<IJobExecutionContext>()));
        _hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>())).Do(_ => _events.Add("capture"));
    }

    [Fact]
    public async Task Invoke_RunsJobInNewScopeAndTrace()
    {
        // Arrange
        var traceId = _scope.PropagationContext.TraceId;
        var sut = new SentryScopeMiddleware(_hub);

        // Act
        await sut.Invoke(MiddlewareTestHelpers.CreateContext<NoOpJob>(), (_, _) =>
        {
            _events.Add("next");
            return default;
        }, CancellationToken.None);

        // Assert
        _events.Should().Equal("push", "next", "dispose");
        _scope.Tags.Should().Contain(SentryScopeMiddleware.JobTag, "DEFAULT.Cleanup");
        _scope.PropagationContext.TraceId.Should().NotBe(traceId);
    }

    [Fact]
    public async Task Invoke_JobThrows_CapturesExceptionInJobScopeAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Job failed");
        var sut = new SentryScopeMiddleware(_hub);

        // Act
        var act = () => sut.Invoke(MiddlewareTestHelpers.CreateContext<NoOpJob>(), MiddlewareTestHelpers.Next(exception), CancellationToken.None).AsTask();

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        _events.Should().Equal("push", "capture", "dispose");
        _hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception == exception));
        exception.Data[Mechanism.MechanismKey].Should().Be(SentryScopeMiddleware.MechanismType);
        exception.Data[Mechanism.HandledKey].Should().Be(false);
        exception.Data[Mechanism.TerminalKey].Should().Be(false);
    }

    [Fact]
    public async Task Invoke_JobCancelled_DoesNotCaptureException()
    {
        // Arrange
        var cancelled = new CancellationToken(canceled: true);
        var sut = new SentryScopeMiddleware(_hub);

        // Act
        var act = () => sut.Invoke(MiddlewareTestHelpers.CreateContext<NoOpJob>(cancelled), MiddlewareTestHelpers.Next(new OperationCanceledException(cancelled)), cancelled).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }
}
