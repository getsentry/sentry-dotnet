using System;
using NSubstitute;
using Sentry.Integrations;
using Xunit;

namespace Sentry.Tests
{
    public class AppDomainUnhandledExceptionIntegrationTests
    {
        private class Fixture
        {
            public IHub Hub { get; set; } = Substitute.For<IHub, IDisposable>();
            public IAppDomain AppDomain { get; set; } = Substitute.For<IAppDomain>();

            public Fixture() => Hub.IsEnabled.Returns(true);

            public AppDomainUnhandledExceptionIntegration GetSut()
                => new AppDomainUnhandledExceptionIntegration(AppDomain);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Handle_WithException_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);

            sut.Handle(this, new UnhandledExceptionEventArgs(new Exception(), true));

            _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Handle_NoException_NoCaptureEvent()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);

            sut.Handle(this, new UnhandledExceptionEventArgs(new object(), true));

            _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Handle_TerminatingTrue_DisposesHub()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);

            sut.Handle(this, new UnhandledExceptionEventArgs(new Exception(), true));

            var disposableHub = _fixture.Hub as IDisposable;
            disposableHub.Received(1).Dispose();
        }

        [Fact]
        public void Handle_TerminatingTrue_NoException_DisposesHub()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);

            sut.Handle(this, new UnhandledExceptionEventArgs(null, true));

            var disposableHub = _fixture.Hub as IDisposable;
            disposableHub.Received(1).Dispose();
        }

        [Fact]
        public void Handle_TerminatingFalse_DoesNotDisposesHub()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);

            sut.Handle(this, new UnhandledExceptionEventArgs(new Exception(), false));

            var disposableHub = _fixture.Hub as IDisposable;
            disposableHub.DidNotReceive().Dispose();
        }

        [Fact]
        public void Register_UnhandledException_Subscribes()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);

            _fixture.AppDomain.Received().UnhandledException += sut.Handle;
        }

        [Fact]
        public void Unregister_UnhandledException_Unsubscribes()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub);
            sut.Unregister(_fixture.Hub);

            _fixture.AppDomain.Received(1).UnhandledException -= sut.Handle;
        }
    }
}
