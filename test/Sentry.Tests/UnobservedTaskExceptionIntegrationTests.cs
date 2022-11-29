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

        // We should have a stack trace and mechanism on the final reported exception
        var reportedException = capturedEvent.SentryExceptions?.LastOrDefault();
        Assert.NotNull(reportedException);
        Assert.NotNull(reportedException.Stacktrace);
        Assert.NotNull(reportedException.Mechanism);
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
