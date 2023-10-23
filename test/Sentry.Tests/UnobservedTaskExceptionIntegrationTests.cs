namespace Sentry.Tests;

public class UnobservedTaskExceptionIntegrationTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub, IDisposable>();
        public IAppDomain AppDomain { get; set; } = Substitute.For<IAppDomain>();

        public Fixture() => Hub.IsEnabled.Returns(true);

        public UnobservedTaskExceptionIntegration GetSut() => new(AppDomain);
    }

    private readonly Fixture _fixture = new();

    private SentryOptions SentryOptions { get; } = new();

    [Fact]
    public void Handle_WithException_CaptureEvent()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnobservedTaskExceptionEventArgs(new AggregateException()));

        _ = _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    // Test is flaky on mobile in CI.
#if !(__MOBILE__ && CI_BUILD)
    [Fact]
    public void Handle_UnobservedTaskException_CaptureEvent()
    {
        _fixture.AppDomain = AppDomainAdapter.Instance;
        var captureCalledEvent = new ManualResetEvent(false);
        SentryEvent capturedEvent = null;
        _fixture.Hub.When(x => x.CaptureEvent(Arg.Any<SentryEvent>()))
            .Do(callInfo =>
            {
                capturedEvent = callInfo.Arg<SentryEvent>();
                captureCalledEvent.Set();
            });

        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);
        var taskStartedEvent = new ManualResetEvent(false);

        // This action wrapper allows this test to work in DEBUG configuration.
        // Without it, the test only works in RELEASE configuration.
        // See https://stackoverflow.com/questions/21266137/test-for-unobserved-exceptions
        var action = () =>
        {
            _ = Task.Run(() =>
            {
                _ = taskStartedEvent.Set();
                throw new Exception("Unhandled on Task");
            });
        };
        action.Invoke();

        Assert.True(taskStartedEvent.WaitOne(TimeSpan.FromSeconds(10)));
        var counter = 0;
        do
        {
            Assert.True(counter++ < 20);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        } while (!captureCalledEvent.WaitOne(TimeSpan.FromMilliseconds(100)));

        // The captured event should have an exception
        var capturedException = capturedEvent.Exception;
        Assert.NotNull(capturedException);

        // Simulate processing the event
        var processors = SentryOptions.GetAllExceptionProcessors();
        foreach (var processor in processors)
        {
            processor.Process(capturedException, capturedEvent);
        }

        // There should be two reported exceptions
        var exceptions = capturedEvent.SentryExceptions?.ToList();
        Assert.NotNull(exceptions);
        Assert.Equal(2, exceptions.Count);

        // The first should be the actual exception that was unobserved.
        var actualException = exceptions[0];
        Assert.NotNull(actualException.Stacktrace);
        Assert.NotNull(actualException.Mechanism);
        Assert.Equal("chained", actualException.Mechanism.Type);
        Assert.Equal("InnerExceptions[0]", actualException.Mechanism.Source);
        Assert.Equal(1, actualException.Mechanism.ExceptionId);
        Assert.Equal(0, actualException.Mechanism.ParentId);
        Assert.False(actualException.Mechanism.IsExceptionGroup);
        Assert.False(actualException.Mechanism.Synthetic);

        // The last should be the aggregate exception that raised the UnobservedTaskException event.
        var aggregateException = exceptions[1];
        Assert.Null(aggregateException.Stacktrace);
        Assert.NotNull(aggregateException.Mechanism);
        Assert.Equal("UnobservedTaskException", aggregateException.Mechanism.Type);
        Assert.Null(aggregateException.Mechanism.Source);
        Assert.Equal(0, aggregateException.Mechanism.ExceptionId);
        Assert.Null(aggregateException.Mechanism.ParentId);
        Assert.True(aggregateException.Mechanism.IsExceptionGroup);
        Assert.False(aggregateException.Mechanism.Synthetic);
    }
#endif

    [Fact]
    public void Register_UnhandledException_Subscribes()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        _fixture.AppDomain.Received().UnobservedTaskException += sut.Handle;
    }
}
