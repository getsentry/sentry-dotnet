using System;
using System.Linq;
using System.Net.Http;
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
        public void CaptureEvent_IdReturnedToString_NoDashes()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent(new Exception());

            var actual = sut.CaptureEvent(evt);

            var hasDashes = actual.ToString().Contains('-');
            Assert.False(hasDashes);
        }

        [Fact]
        public void CaptureEvent_ExceptionProcessorsOnOptions_Invoked()
        {
            var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
            _fixture.SentryOptions.AddExceptionProcessorProvider(() => new[] { exceptionProcessor });
            var sut = _fixture.GetSut();

            var evt = new SentryEvent(new Exception());

            sut.CaptureEvent(evt);

            exceptionProcessor.Received(1).Process(evt.Exception, evt);
        }

        [Fact]
        public void CaptureEvent_ExceptionProcessorsOnScope_Invoked()
        {
            var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
            var scope = new Scope();
            scope.AddExceptionProcessor(exceptionProcessor);

            var sut = _fixture.GetSut();

            var evt = new SentryEvent(new Exception());

            sut.CaptureEvent(evt, scope);

            exceptionProcessor.Received(1).Process(evt.Exception, evt);
        }

        [Fact]
        public void CaptureEvent_NullEventWithScope_EmptyGuid()
        {
            var sut = _fixture.GetSut();
            Assert.Equal(default, sut.CaptureEvent(null, new Scope(_fixture.SentryOptions)));
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
            var expectedEvent = new SentryEvent(id: expectedId);
            _fixture.BackgroundWorker.EnqueueEvent(expectedEvent).Returns(true);

            var sut = _fixture.GetSut();

            var actualId = sut.CaptureEvent(expectedEvent);
            Assert.Equal(expectedId, (Guid)actualId);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_QueuesEvent()
        {
            var expectedId = Guid.NewGuid();
            var expectedEvent = new SentryEvent(id: expectedId);
            _fixture.BackgroundWorker.EnqueueEvent(expectedEvent).Returns(true);

            var sut = _fixture.GetSut();

            var actualId = sut.CaptureEvent(expectedEvent, new Scope(_fixture.SentryOptions));
            Assert.Equal(expectedId, (Guid)actualId);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_EvaluatesScope()
        {
            var scope = new Scope(_fixture.SentryOptions);
            var sut = _fixture.GetSut();

            var evaluated = false;
            object actualSender = null;
            scope.OnEvaluating += (sender, args) =>
            {
                actualSender = sender;
                evaluated = true;
            };

            sut.CaptureEvent(new SentryEvent(), scope);

            Assert.True(evaluated);
            Assert.Same(scope, actualSender);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_CopyScopeIntoEvent()
        {
            const string expectedBreadcrumb = "test";
            var scope = new Scope(_fixture.SentryOptions);
            scope.AddBreadcrumb(expectedBreadcrumb);
            var @event = new SentryEvent();

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event, scope);

            Assert.Equal(scope.InternalBreadcrumbs, @event.InternalBreadcrumbs);
        }

        [Fact]
        public void CaptureEvent_BeforeEvent_RejectEvent()
        {
            _fixture.SentryOptions.BeforeSend = @event => null;
            var expectedEvent = new SentryEvent();

            var sut = _fixture.GetSut();
            var actualId = sut.CaptureEvent(expectedEvent, new Scope(_fixture.SentryOptions));

            Assert.Equal(default, actualId);
            _fixture.BackgroundWorker.DidNotReceive().EnqueueEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void CaptureEvent_BeforeEvent_ModifyEvent()
        {
            SentryEvent received = null;
            _fixture.SentryOptions.BeforeSend = e => received = e;

            var @event = new SentryEvent();

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event);

            Assert.Same(@event, received);
        }

        [Fact]
        public void CaptureEvent_LevelOnScope_OverridesLevelOnEvent()
        {
            const SentryLevel expected = SentryLevel.Fatal;
            var @event = new SentryEvent
            {
                Level = SentryLevel.Fatal
            };
            var scope = new Scope
            {
                Level = expected
            };

            var sut = _fixture.GetSut();
            sut.CaptureEvent(@event, scope);

            Assert.Equal(expected, @event.Level);
        }

        [Fact]
        public void CaptureEvent_SamplingLowest_DropsEvent()
        {
            // Three decimal places longer than what Random returns. Should always drop
            _fixture.SentryOptions.SampleRate = 0.00000000000000000001f;
            var @event = new SentryEvent();

            var sut = _fixture.GetSut();

            Assert.Equal(default, sut.CaptureEvent(@event));
        }

        [Fact]
        public void CaptureEvent_SamplingHighest_SendsEvent()
        {
            // Three decimal places longer than what Random returns. Should always send
            _fixture.SentryOptions.SampleRate = 0.99999999999999999999f;
            SentryEvent received = null;
            _fixture.SentryOptions.BeforeSend = e => received = e;

            var @event = new SentryEvent();

            var sut = _fixture.GetSut();

            sut.CaptureEvent(@event);

            Assert.Same(@event, received);
        }

        [Fact]
        public void CaptureEvent_SamplingNull_DropEvent()
        {
            _fixture.SentryOptions.SampleRate = null;
            SentryEvent received = null;
            _fixture.SentryOptions.BeforeSend = e => received = e;

            var @event = new SentryEvent();

            var sut = _fixture.GetSut();

            sut.CaptureEvent(@event);

            Assert.Same(@event, received);
        }

        [Fact]
        public void CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
        {
            var error = new Exception("Exception message!");
            _fixture.SentryOptions.BeforeSend = e => throw error;

            var @event = new SentryEvent();

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

            var @event = new SentryEvent();

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
        public void Ctor_HttpOptionsCallback_InvokedConfigureClient()
        {
            var invoked = false;
            _fixture.BackgroundWorker = null;
            _fixture.SentryOptions.Dsn = DsnSamples.Valid;
            _fixture.SentryOptions.ConfigureClient = (client, dsn) => invoked = true;

            using (_fixture.GetSut())
            {
                Assert.True(invoked);
            }
        }

        [Fact]
        public void Ctor_HttpOptionsCallback_InvokedConfigureHandler()
        {
            var invoked = false;
            _fixture.BackgroundWorker = null;
            _fixture.SentryOptions.Dsn = DsnSamples.Valid;
#pragma warning disable 618 // Tests will be removed once obsolete code gets removed
            _fixture.SentryOptions.ConfigureHandler = (handler, dsn) => invoked = true;
#pragma warning restore 618

            using (_fixture.GetSut())
            {
                Assert.True(invoked);
            }
        }

        [Fact]
        public void Ctor_CreateHttpClientHandler_InvokedConfigureHandler()
        {
            var invoked = false;
            _fixture.BackgroundWorker = null;
            _fixture.SentryOptions.Dsn = DsnSamples.Valid;
            _fixture.SentryOptions.CreateHttpClientHandler = (dsn) =>
            {
                invoked = true;
                return Substitute.For<HttpClientHandler>();
            };

            using (_fixture.GetSut())
            {
                Assert.True(invoked);
            }
        }

        [Fact]
        public void Ctor_NullBackgroundWorker_ConcreteBackgroundWorker()
        {
            _fixture.SentryOptions.Dsn = DsnSamples.Valid;

            using (var sut = new SentryClient(_fixture.SentryOptions))
            {
                Assert.IsType<BackgroundWorker>(sut.Worker);
            }
        }
    }
}
