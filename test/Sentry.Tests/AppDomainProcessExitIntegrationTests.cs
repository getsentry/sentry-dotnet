using System;
using System.Threading.Tasks;
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

            public AppDomainProcessExitIntegration GetSut() => new AppDomainProcessExitIntegration(AppDomain);
        }

        private readonly Fixture _fixture = new Fixture();
        public SentryOptions SentryOptions { get; set; } = new SentryOptions();

        [Fact]
        public async Task Handle_WithException_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            await sut.FlushAsync(this, EventArgs.Empty).ConfigureAwait(false);

            await _fixture.Hub.Received(1).FlushAsync(Arg.Any<TimeSpan>());
        }

        [Fact]
        public void Register_ProcessExit_Subscribes()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            _fixture.AppDomain.Received().ProcessExit += sut.HandlerAsync;
        }

        [Fact]
        public void Unregister_ProcessExit_Unsubscribes()
        {
            var sut = _fixture.GetSut();

            sut.Register(_fixture.Hub, SentryOptions);
            sut.Unregister(_fixture.Hub);

            _fixture.AppDomain.Received(1).ProcessExit -= sut.HandlerAsync;
        }
    }
}
