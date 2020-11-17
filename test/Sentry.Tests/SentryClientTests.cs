using System;
using System.Linq;
using System.Net.Http;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;
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
        public void CaptureEvent_ExceptionFiltered_EmptySentryId()
        {
            _fixture.SentryOptions.AddExceptionFilterForType<SystemException>();
            _ = _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

            var sut = _fixture.GetSut();

            // Filtered out for it's the exact filtered type
            Assert.Equal(default, sut.CaptureException(new SystemException()));
            _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

            // Filtered for it's a derived type
            Assert.Equal(default, sut.CaptureException(new ArithmeticException()));
            _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());

            // Not filtered since it's not in the inheritance chain
            Assert.NotEqual(default, sut.CaptureException(new Exception()));
            _ = _fixture.BackgroundWorker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
        }

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

            _ = sut.CaptureEvent(evt);

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

            _ = sut.CaptureEvent(evt, scope);

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
            var expectedEvent = new SentryEvent(eventId: expectedId);
            _ = _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

            var sut = _fixture.GetSut();

            var actualId = sut.CaptureEvent(expectedEvent);
            Assert.Equal(expectedId, (Guid)actualId);
        }

        [Fact]
        public void CaptureEvent_EventAndScope_QueuesEvent()
        {
            var expectedId = Guid.NewGuid();
            var expectedEvent = new SentryEvent(eventId: expectedId);
            _ = _fixture.BackgroundWorker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

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

            _ = sut.CaptureEvent(new SentryEvent(), scope);

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
            _ = sut.CaptureEvent(@event, scope);

            Assert.Equal(scope.Breadcrumbs, @event.Breadcrumbs);
        }

        [Fact]
        public void CaptureEvent_BeforeEvent_RejectEvent()
        {
            _fixture.SentryOptions.BeforeSend = @event => null;
            var expectedEvent = new SentryEvent();

            var sut = _fixture.GetSut();
            var actualId = sut.CaptureEvent(expectedEvent, new Scope(_fixture.SentryOptions));

            Assert.Equal(default, actualId);
            _ = _fixture.BackgroundWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
        }

        [Fact]
        public void CaptureEvent_BeforeEvent_ModifyEvent()
        {
            SentryEvent received = null;
            _fixture.SentryOptions.BeforeSend = e => received = e;

            var @event = new SentryEvent();

            var sut = _fixture.GetSut();
            _ = sut.CaptureEvent(@event);

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
            _ = sut.CaptureEvent(@event, scope);

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

            _ = sut.CaptureEvent(@event);

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

            _ = sut.CaptureEvent(@event);

            Assert.Same(@event, received);
        }

        [Fact]
        public void CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
        {
            var error = new Exception("Exception message!");
            _fixture.SentryOptions.BeforeSend = e => throw error;

            var @event = new SentryEvent();

            var sut = _fixture.GetSut();
            _ = sut.CaptureEvent(@event);

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
            _ = sut.CaptureEvent(@event);

            Assert.Equal(expectedRelease, @event.Release);
        }

        [Fact]
        public void CaptureEvent_DisposedClient_ThrowsObjectDisposedException()
        {
            var sut = _fixture.GetSut();
            sut.Dispose();
            _ = Assert.Throws<ObjectDisposedException>(() => sut.CaptureEvent(null));
        }

        [Fact]
        public void CaptureUserFeedback_EventIdEmpty_IgnoreUserFeedback()
        {
            //Arrange
            var sut = _fixture.GetSut();

            //Act
            sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "email", "comment"));

            //Assert
            _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
        }

        [Fact]
        public void CaptureUserFeedback_ValidUserFeedback_FeedbackRegistered()
        {
            //Arrange
            var sut = _fixture.GetSut();

            //Act
            sut.CaptureUserFeedback(new UserFeedback(Guid.Parse("4eb98e5f861a41019f270a7a27e84f02"), "email", "comment"));

            //Assert
            _ = sut.Worker.Received(1).EnqueueEnvelope(Arg.Any<Envelope>());
        }

        [Fact]
        public void CaptureUserFeedback_CommentsNullOrEmpty_FeedbackIgnored()
        {
            //Arrange
            var sut = _fixture.GetSut();

            //Act
            sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "email", null));
            sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "email", ""));

            //Assert
            _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
        }

        [Fact]
        public void CaptureUserFeedback_EmailNullOrEmpty_FeedbackIgnored()
        {
            //Arrange
            var sut = _fixture.GetSut();

            //Act
            sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, null, "comment"));
            sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "", "comment"));

            //Assert
            _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
        }

        [Fact]
        public void CaptureUserFeedback_EventIdEmpty_FeedbackIgnored()
        {

            //Arrange
            var sut = _fixture.GetSut();

            //Act
            sut.CaptureUserFeedback(new UserFeedback(SentryId.Empty, "email", "comment"));

            //Assert
            _ = sut.Worker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
        }

        [Fact]
        public void CaptureUserFeedback_DisposedClient_ThrowsObjectDisposedException()
        {
            var sut = _fixture.GetSut();
            sut.Dispose();
            _ = Assert.Throws<ObjectDisposedException>(() => sut.CaptureUserFeedback(null));
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
            _fixture.SentryOptions.Dsn = DsnSamples.ValidDsnWithSecret;
            _fixture.SentryOptions.ConfigureClient = (client) => invoked = true;

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
            _fixture.SentryOptions.Dsn = DsnSamples.ValidDsnWithSecret;
            _fixture.SentryOptions.CreateHttpClientHandler = () =>
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
            _fixture.SentryOptions.Dsn = DsnSamples.ValidDsnWithSecret;

            using (var sut = new SentryClient(_fixture.SentryOptions))
            {
                _ = Assert.IsType<BackgroundWorker>(sut.Worker);
            }
        }
    }
}
