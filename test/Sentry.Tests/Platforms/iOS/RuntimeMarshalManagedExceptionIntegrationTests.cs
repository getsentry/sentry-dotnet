#if IOS
using ObjCRuntime;
using Sentry.Cocoa;

namespace Sentry.Tests.Platforms.iOS;

public class RuntimeMarshalManagedExceptionIntegrationTests
{
    private class Fixture
    {
        public IHub Hub { get; } = Substitute.For<IHub, IDisposable>();
        public IRuntime Runtime { get; } = Substitute.For<IRuntime>();

        public RuntimeMarshalManagedExceptionIntegration GetSut() => new(Runtime);
    }

    private readonly Fixture _fixture = new();
    private SentryOptions SentryOptions { get; } = new();

    [Fact]
    public void Handle_WithException_CaptureEvent()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new MarshalManagedExceptionEventArgs { Exception = new Exception() });

        _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Handle_WithException_IsHandledFalse()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        var exception = new Exception();
        sut.Handle(this, new MarshalManagedExceptionEventArgs { Exception = exception });
        Assert.Equal(false, exception.Data[Mechanism.HandledKey]);
        Assert.True(exception.Data.Contains(Mechanism.MechanismKey));

        var stackTraceFactory = Substitute.For<ISentryStackTraceFactory>();
        var exceptionProcessor = new MainExceptionProcessor(SentryOptions, () => stackTraceFactory);
        var @event = new SentryEvent(exception);

        exceptionProcessor.Process(exception, @event);
        Assert.NotNull(@event.SentryExceptions?.ToList().Single(p => p.Mechanism?.Handled == false));
    }

    [Fact]
    public void Handle_NoException_NoCaptureEvent()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new MarshalManagedExceptionEventArgs());

        _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Register_UnhandledException_Subscribes()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        _fixture.Runtime.Received().MarshalManagedException += sut.Handle;
    }
}
#endif
