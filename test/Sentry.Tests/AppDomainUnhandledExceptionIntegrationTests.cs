namespace Sentry.Tests;

public class AppDomainUnhandledExceptionIntegrationTests
{
    private class Fixture
    {
        public IHub Hub { get; } = Substitute.For<IHub, IDisposable>();
        public IAppDomain AppDomain { get; } = Substitute.For<IAppDomain>();

        public AppDomainUnhandledExceptionIntegration GetSut() => new(AppDomain);
    }

    private readonly Fixture _fixture = new();
    private SentryOptions SentryOptions { get; } = new();

    [Fact]
    public void Handle_WithException_CaptureEvent()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnhandledExceptionEventArgs(new Exception(), true));

        _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Handle_NoException_NoCaptureEvent()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnhandledExceptionEventArgs(new object(), true));

        _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Handle_TerminatingTrue_FlushesHub()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnhandledExceptionEventArgs(new Exception(), true));

        _fixture.Hub.Received(1).FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public void Handle_TerminatingTrue_IsHandledFalse()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        var exception = new Exception();
        sut.Handle(this, new UnhandledExceptionEventArgs(exception, true));
        Assert.Equal(false, exception.Data[Mechanism.HandledKey]);
        Assert.True(exception.Data.Contains(Mechanism.MechanismKey));

        var stackTraceFactory = Substitute.For<ISentryStackTraceFactory>();
        var exceptionProcessor = new MainExceptionProcessor(SentryOptions, () => stackTraceFactory);
        var @event = new SentryEvent(exception);

        exceptionProcessor.Process(exception, @event);
        Assert.NotNull(@event.SentryExceptions?.ToList().Single(p => p.Mechanism?.Handled == false));
    }

    [Fact]
    public void Handle_TerminatingTrue_NoException_FlushesHub()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnhandledExceptionEventArgs(null!, true));

        _fixture.Hub.Received(1).FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public void Handle_TerminatingFalse_DoesNotDisposesHub()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new UnhandledExceptionEventArgs(new Exception(), false));

        var disposableHub = _fixture.Hub as IDisposable;
        disposableHub.DidNotReceive().Dispose();
    }

    [Fact]
    public void Register_UnhandledException_Subscribes()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        _fixture.AppDomain.Received().UnhandledException += sut.Handle;
    }
}
