using System;
using NSubstitute;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class SentryClientExtensionsTests
    {
        private readonly ISentryClient _sut = Substitute.For<ISentryClient>();

        [Fact]
        public void CaptureException_DisabledClient_DoesNotCaptureEvent()
        {
            _sut.IsEnabled.Returns(false);
            var id = _sut.CaptureException(new Exception());

            _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }

        [Fact]
        public void CaptureException_EnabledClient_CapturesEvent()
        {
            _sut.IsEnabled.Returns(true);
            _sut.CaptureException(new Exception());
            _sut.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void CaptureMessage_DisabledClient_DoesNotCaptureEvent()
        {
            _sut.IsEnabled.Returns(false);
            var id = _sut.CaptureMessage("Message");

            _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }

        [Fact]
        public void CaptureMessage_EnabledClient_CapturesEvent()
        {
            _sut.IsEnabled.Returns(true);
            _sut.CaptureMessage("Message");
            _sut.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void CaptureMessage_Level_CapturesEventWithLevel()
        {
            const SentryLevel expectedLevel = SentryLevel.Fatal;
            _sut.IsEnabled.Returns(true);
            _sut.CaptureMessage("Message", expectedLevel);
            _sut.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == expectedLevel));
        }

        [Fact]
        public void CaptureMessage_Message_CapturesEventWithMessage()
        {
            const string expectedMessage = "message";
            _sut.IsEnabled.Returns(true);
            _sut.CaptureMessage(expectedMessage);
            _sut.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Message == expectedMessage));
        }

        [Fact]
        public void CaptureMessage_WhitespaceMessage_DoesNotCapturesEventWithMessage()
        {
            _sut.IsEnabled.Returns(true);
            var id = _sut.CaptureMessage("   ");

            _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }

        [Fact]
        public void CaptureMessage_NullMessage_DoesNotCapturesEventWithMessage()
        {
            _sut.IsEnabled.Returns(true);
            var id = _sut.CaptureMessage(null);

            _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }
    }
}
