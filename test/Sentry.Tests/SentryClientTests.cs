using System;
using System.Linq;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class SentryClientTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();
            public IBackgroundWorker BackgroundWorker { get; set; } = Substitute.For<IBackgroundWorker, IDisposable>();

            public SentryClient GetSut() => new SentryClient(SentryOptions, BackgroundWorker);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void CaptureEvent_NullEventWithScope_EmptyGuid()
        {
            var sut = _fixture.GetSut();
            Assert.Equal(default, sut.CaptureEvent(null, new Scope()));
        }

        [Fact]
        public void CaptureEvent_NullEvent_EmptyGuid()
        {
            var sut = _fixture.GetSut();
            Assert.Equal(default, sut.CaptureEvent(null));
        }

        [Fact]
        public void CaptureEvent_NullScope_QueuesEvent()
        {
            var expectedId = Guid.NewGuid();
            var expectedEvent = new SentryEvent(id: expectedId, populate: false);
            _fixture.BackgroundWorker.EnqueueEvent(expectedEvent).Returns(true);

            var sut = _fixture.GetSut();

            var actualId = sut.CaptureEvent(expectedEvent);
            Assert.Equal(expectedId, actualId);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_QueuesEvent()
        {
            var expectedId = Guid.NewGuid();
            var expectedEvent = new SentryEvent(id: expectedId, populate: false);
            _fixture.BackgroundWorker.EnqueueEvent(expectedEvent).Returns(true);

            var sut = _fixture.GetSut();

            var actualId = sut.CaptureEvent(expectedEvent, new Scope());
            Assert.Equal(expectedId, actualId);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_EvaluatesScope()
        {
            var scope = new Scope();
            var sut = _fixture.GetSut();

            var evaluated = false;
            object actualSender = null;
            scope.OnEvaluating += (sender, args) =>
            {
                actualSender = sender;
                evaluated = true;
            };

            sut.CaptureEvent(new SentryEvent(populate: false), scope);

            Assert.True(evaluated);
            Assert.Same(scope, actualSender);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_CopyScopeIntoEvent()
        {
            const string expectedBreadcrumb = "test";
            var scope = new Scope();
            scope.AddBreadcrumb(expectedBreadcrumb);
            var @event = new SentryEvent(populate: false);

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event, scope);

            Assert.Same(scope.InternalBreadcrumbs, @event.InternalBreadcrumbs);
        }

        [Fact]
        public void CaptureEvent_BeforeEvent_RejectEvent()
        {
            _fixture.SentryOptions.BeforeSend = @event => null;
            var expectedEvent = new SentryEvent(populate: false);

            var sut = _fixture.GetSut();
            var actualId = sut.CaptureEvent(expectedEvent, new Scope());

            Assert.Equal(default, actualId);
            _fixture.BackgroundWorker.DidNotReceive().EnqueueEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void CaptureEvent_BeforeEvent_ModifyEvent()
        {
            SentryEvent received = null;
            _fixture.SentryOptions.BeforeSend = e => received = e;

            var @event = new SentryEvent(populate: false);

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event);

            Assert.Same(@event, received);
        }

        [Fact]
        public void CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
        {
            var error = new Exception("Exception message!");
            _fixture.SentryOptions.BeforeSend = e => throw error;

            var @event = new SentryEvent(populate: false);

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event);

            var crumb = @event.Breadcrumbs.First();
            Assert.Equal("BeforeSend callback failed.", crumb.Message);
            Assert.Equal(error.Message, crumb.Data["message"]);
            Assert.Equal(error.StackTrace, crumb.Data["stackTrace"]);
        }

        [Fact]
        public void CaptureEvent_Release_SetFromOptions()
        {
            const string expectedRelease = "release number";
            _fixture.SentryOptions.Release = expectedRelease;

            var @event = new SentryEvent(populate: false);

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event);

            Assert.Equal(expectedRelease, @event.Release);
        }

        [Fact]
        public void CaptureEvent_DisposedClient_ThrowsObjectDisposedException()
        {
            var sut = _fixture.GetSut();
            sut.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sut.CaptureEvent(null));
        }

        [Fact]
        public void Dispose_Worker_DisposeCalled()
        {
            _fixture.GetSut().Dispose();
            (_fixture.BackgroundWorker as IDisposable).Received(1).Dispose();
        }

        [Fact]
        public void Dispose_MultipleCalls_WorkerDisposedOnce()
        {
            var sut = _fixture.GetSut();
            sut.Dispose();
            sut.Dispose();
            (_fixture.BackgroundWorker as IDisposable).Received(1).Dispose();
        }

        [Fact]
        public void Dispose_WorkerDoesNotImplementDispose_DoesntThrow()
        {
            _fixture.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            _fixture.GetSut().Dispose();
        }

        [Fact]
        public void IsEnabled_AlwaysTrue()
        {
            var sut = _fixture.GetSut();
            Assert.True(sut.IsEnabled);
        }

        [Fact]
        public void Ctor_NullSentryOptions_ThrowsArgumentNullException()
        {
            _fixture.SentryOptions = null;
            var ex = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Ctor_NullBackgrondWorker_ConcreteBackgroundWorker()
        {
            _fixture.SentryOptions.Dsn = DsnSamples.Valid;

            using (var sut = new SentryClient(_fixture.SentryOptions))
            {
                Assert.IsType<BackgroundWorker>(sut.Worker);
            }
        }
    }
}
