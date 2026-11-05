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

    [Fact]
    public void Handle_ThrowObjectiveCException_ChangesModeToAbort()
    {
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);
        var args = new MarshalManagedExceptionEventArgs { Exception = new Exception(), ExceptionMode = MarshalManagedExceptionMode.ThrowObjectiveCException };

        sut.Handle(this, args);

        Assert.Equal(MarshalManagedExceptionMode.Abort, args.ExceptionMode);
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

    [Theory]
    [InlineData(true, MarshalManagedExceptionMode.UnwindNativeCode)]
    [InlineData(false, MarshalManagedExceptionMode.ThrowObjectiveCException)]
    public void Handle_EscapedNativeScopeCallback_ReturnsExceptionToCaller(bool isMono, MarshalManagedExceptionMode expectedMode)
    {
        _fixture.Runtime.IsMono.Returns(isMono);
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);
        var args = new MarshalManagedExceptionEventArgs { Exception = new Exception(), ExceptionMode = MarshalManagedExceptionMode.Default };

        CocoaScope.Configure(_ => { }, configureNativeScope: _ => sut.Handle(this, args));

        _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal(expectedMode, args.ExceptionMode);
        _fixture.Runtime.DidNotReceive().IgnoreNextSignal(Arg.Any<int>());
    }

    [Fact]
    public void Handle_CaptureThrows_LeavesModeUnchanged()
    {
        _fixture.Hub.When(hub => hub.CaptureEvent(Arg.Any<SentryEvent>())).Do(_ => throw new InvalidOperationException());
        var sut = _fixture.GetSut();
        sut.Register(_fixture.Hub, SentryOptions);
        var args = new MarshalManagedExceptionEventArgs { Exception = new Exception(), ExceptionMode = MarshalManagedExceptionMode.Default };

        sut.Handle(this, args);

        Assert.Equal(MarshalManagedExceptionMode.Default, args.ExceptionMode);
        _fixture.Runtime.DidNotReceive().IgnoreNextSignal(Arg.Any<int>());
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
