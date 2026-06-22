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

    [Theory]
    [InlineData(MarshalManagedExceptionMode.Default)]
    [InlineData(MarshalManagedExceptionMode.ThrowObjectiveCException)]
    [InlineData(MarshalManagedExceptionMode.Abort)]
    public void Handle_Mono_AbortingMode_IgnoresSigabrt(MarshalManagedExceptionMode mode)
    {
        _fixture.Runtime.IsMono.Returns(true);
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new MarshalManagedExceptionEventArgs { Exception = new Exception(), ExceptionMode = mode });

        _fixture.Runtime.Received(1).IgnoreNextSignal(6);
    }

    [Theory]
    [InlineData(MarshalManagedExceptionMode.Disable)]
    [InlineData(MarshalManagedExceptionMode.UnwindNativeCode)]
    public void Handle_Mono_NonAbortingMode_DoesNotIgnoreSigabrt(MarshalManagedExceptionMode mode)
    {
        _fixture.Runtime.IsMono.Returns(true);
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new MarshalManagedExceptionEventArgs { Exception = new Exception(), ExceptionMode = mode });

        _fixture.Runtime.DidNotReceive().IgnoreNextSignal(Arg.Any<int>());
    }

    [Theory]
    [InlineData(MarshalManagedExceptionMode.Default)]
    [InlineData(MarshalManagedExceptionMode.Disable)]
    [InlineData(MarshalManagedExceptionMode.UnwindNativeCode)]
    [InlineData(MarshalManagedExceptionMode.ThrowObjectiveCException)]
    [InlineData(MarshalManagedExceptionMode.Abort)]
    public void Handle_CoreCLR_AnyMode_IgnoresSigabrt(MarshalManagedExceptionMode mode)
    {
        _fixture.Runtime.IsMono.Returns(false);
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new MarshalManagedExceptionEventArgs { Exception = new Exception(), ExceptionMode = mode });

        _fixture.Runtime.Received(1).IgnoreNextSignal(6);
    }

    [Fact]
    public void Handle_NoException_DoesNotIgnoreSigabrt()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);

        sut.Handle(this, new MarshalManagedExceptionEventArgs());

        _fixture.Runtime.DidNotReceive().IgnoreNextSignal(Arg.Any<int>());
    }
}
#endif
