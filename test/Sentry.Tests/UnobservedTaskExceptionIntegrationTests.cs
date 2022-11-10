namespace Sentry.Tests;

public class UnobservedTaskExceptionIntegrationTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub, IDisposable>();
        public IAppDomain AppDomain { get; set; } = Substitute.For<IAppDomain>();

        public Fixture() => Hub.IsEnabled.Returns(true);

        public UnobservedTaskExceptionIntegration GetSut()
            => new(AppDomain);
    }

    private readonly Fixture _fixture = new();
    public SentryOptions SentryOptions { get; set; } = new();

    [Fact]
    public void Handle_WithException_CaptureEvent()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnobservedTaskExceptionEventArgs(new AggregateException()));

        _ = _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    // Only triggers in release mode.
#if RELEASE
    [SkippableFact]
    public void Handle_UnobservedTaskException_CaptureEvent()
    {
#if __MOBILE__ && CI_BUILD
        throw new Xunit.SkipException("Test is flaky on mobile in CI.");
#else

        _fixture.AppDomain = AppDomainAdapter.Instance;
        var captureCalledEvent = new ManualResetEvent(false);
        _fixture.Hub.When(x => x.CaptureEvent(Arg.Any<SentryEvent>()))
            .Do(_ => captureCalledEvent.Set());

        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);
        var taskStartedEvent = new ManualResetEvent(false);
        _ = Task.Run(() =>
        {
            _ = taskStartedEvent.Set();
            throw new Exception("Unhandled on Task");
        });
        Assert.True(taskStartedEvent.WaitOne(TimeSpan.FromSeconds(10)));
        var counter = 0;
        do
        {
            Assert.True(counter++ < 20);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        } while (!captureCalledEvent.WaitOne(TimeSpan.FromMilliseconds(100)));
#endif
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
