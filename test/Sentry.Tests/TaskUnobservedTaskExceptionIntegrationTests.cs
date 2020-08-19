using System;
using NSubstitute;
using Sentry.Integrations;
using Sentry.Internal;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Sentry.Tests
{
    public class TaskUnobservedTaskExceptionIntegrationTests
    {
        private class Fixture
        {
            public IHub Hub { get; set; } = Substitute.For<IHub, IDisposable>();
            public IAppDomain AppDomain { get; set; } = Substitute.For<IAppDomain>();

            public Fixture() => Hub.IsEnabled.Returns(true);

            public TaskUnobservedTaskExceptionIntegration GetSut()
                => new TaskUnobservedTaskExceptionIntegration(AppDomain);
        }

        private readonly Fixture _fixture = new Fixture();
        public SentryOptions SentryOptions { get; set; } = new SentryOptions();

        [Fact]
        public void Handle_WithException_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            sut.Handle(this, new UnobservedTaskExceptionEventArgs(new AggregateException()));

            _ = _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

#if RELEASE
        [Fact] // Integration test.
        public void Handle_UnobservedTaskException_CaptureEvent()
        {
            // UnobservedTaskException is only registered in release mode.
            // Disabling the test in debug otherwise it'll say it has errors.
            bool finished;
            _fixture.AppDomain = AppDomainAdapter.Instance;
            var evt = new ManualResetEvent(false);
            _fixture.Hub.When(x => x.CaptureEvent(Arg.Any<SentryEvent>()))
                .Do(_ => evt.Set());

            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);
            try
            {
                Task.Factory.StartNew(() => { throw new Exception(""); });
                using (var autoresetEvent = new AutoResetEvent(false))
                {
                    //wait for task to run
                    autoresetEvent.WaitOne(TimeSpan.FromSeconds(2));
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                finished = true;
                Assert.True(evt.WaitOne(TimeSpan.FromMilliseconds(1000)));
            }
            finally
            {
                sut.Unregister(_fixture.Hub);
            }
            Assert.True(finished);
        }
#endif

        [Fact]
        public void Handle_NoException_NoCaptureEvent()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            sut.Handle(this, new UnobservedTaskExceptionEventArgs(null));

            _ = _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Register_UnhandledException_Subscribes()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);

            _fixture.AppDomain.Received().UnobservedTaskException += sut.Handle;
        }

        [Fact]
        public void Unregister_UnhandledException_Unsubscribes()
        {
            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);
            sut.Unregister(_fixture.Hub);

            _fixture.AppDomain.Received(1).UnobservedTaskException -= sut.Handle;
        }
    }
}
