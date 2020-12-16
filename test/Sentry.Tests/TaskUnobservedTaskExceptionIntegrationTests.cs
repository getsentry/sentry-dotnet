using System;
#if RELEASE
using System.Threading;
#endif
using NSubstitute;
using Sentry.Integrations;
using Sentry.Internal;
using Xunit;
using System.Threading.Tasks;

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
        [Fact] // Integration test.
        public void Handle_UnobservedTaskException_CaptureEvent()
        {
            _fixture.AppDomain = AppDomainAdapter.Instance;
            var captureCalledEvent = new ManualResetEvent(false);
            _fixture.Hub.When(x => x.CaptureEvent(Arg.Any<SentryEvent>()))
                .Do(_ => captureCalledEvent.Set());

            var sut = _fixture.GetSut();
            sut.Register(_fixture.Hub, SentryOptions);
            try
            {
                var taskStartedEvent = new ManualResetEvent(false);
                _ = Task.Run(() =>
                {
                    _ = taskStartedEvent.Set();
                    throw new Exception("Unhandled on Task");
                });
                Assert.True(taskStartedEvent.WaitOne(TimeSpan.FromSeconds(4)));
                var counter = 0;
                do
                {
                    Assert.True(counter++ < 10);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                } while (!captureCalledEvent.WaitOne(TimeSpan.FromMilliseconds(100)));
            }
            finally
            {
                sut.Unregister(_fixture.Hub);
            }
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
