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
            _ = _sut.IsEnabled.Returns(false);
            var id = _sut.CaptureException(new Exception());

            _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }

        [Fact]
        public void CaptureException_EnabledClient_CapturesEvent()
        {
            _ = _sut.IsEnabled.Returns(true);
            _ = _sut.CaptureException(new Exception());
            _ = _sut.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void CaptureMessage_DisabledClient_DoesNotCaptureEvent()
        {
            _ = _sut.IsEnabled.Returns(false);
            var id = _sut.CaptureMessage("Message");

            _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }

        [Fact]
        public void CaptureMessage_EnabledClient_CapturesEvent()
        {
            _ = _sut.IsEnabled.Returns(true);
            _ = _sut.CaptureMessage("Message");
            _ = _sut.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void CaptureMessage_Level_CapturesEventWithLevel()
        {
            const SentryLevel expectedLevel = SentryLevel.Fatal;
            _ = _sut.IsEnabled.Returns(true);
            _ = _sut.CaptureMessage("Message", expectedLevel);
            _ = _sut.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == expectedLevel));
        }

        [Fact]
        public void CaptureMessage_Message_CapturesEventWithMessage()
        {
            const string expectedMessage = "message";
            _ = _sut.IsEnabled.Returns(true);
            _ = _sut.CaptureMessage(expectedMessage);
            _ = _sut.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Message == expectedMessage));
        }

        [Fact]
        public void CaptureMessage_WhitespaceMessage_DoesNotCapturesEventWithMessage()
        {
            _ = _sut.IsEnabled.Returns(true);
            var id = _sut.CaptureMessage("   ");

            _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }

        [Fact]
        public void CaptureMessage_NullMessage_DoesNotCapturesEventWithMessage()
        {
            _ = _sut.IsEnabled.Returns(true);
            var id = _sut.CaptureMessage(null);

            _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
            Assert.Equal(default, id);
        }
    }
}
