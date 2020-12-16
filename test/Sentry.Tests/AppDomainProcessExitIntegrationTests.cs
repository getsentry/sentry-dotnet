using System;
using NSubstitute;
using Sentry.Integrations;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests
{
    public class AppDomainProcessExitIntegrationTests
    {
        private class Fixture
        {
            public IHub Hub { get; set; } = Substitute.For<IHub, IDisposable>();

            public IAppDomain AppDomain { get; set; } = Substitute.For<IAppDomain>();

            public Fixture() => Hub.IsEnabled.Returns(true);

            public AppDomainProcessExitIntegration GetSut() => new(AppDomain);
        }

        private readonly Fixture _fixture = new();

        public SentryOptions SentryOptions { get; set; } = new();

        [Fact]
        public void Handle_WithException_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            sut.HandleProcessExit(this, EventArgs.Empty);

            (_fixture.Hub as IDisposable).Received(1).Dispose();
        }

        [Fact]
        public void Register_ProcessExit_Subscribes()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            _fixture.AppDomain.Received().ProcessExit += sut.HandleProcessExit;
        }

        [Fact]
        public void Unregister_ProcessExit_Unsubscribes()
        {
            var sut = _fixture.GetSut();

            sut.Register(_fixture.Hub, SentryOptions);
            sut.Unregister(_fixture.Hub);

            _fixture.AppDomain.Received(1).ProcessExit -= sut.HandleProcessExit;
        }
    }
}
