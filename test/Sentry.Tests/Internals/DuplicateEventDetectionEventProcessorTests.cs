using System;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class DuplicateEventDetectionEventProcessorTests
    {
        private readonly DuplicateEventDetectionEventProcessor _sut = new DuplicateEventDetectionEventProcessor();

        [Fact]
        public void Process_DuplicateEvent_Returns()
        {
            var @event = new SentryEvent();

            _ = _sut.Process(@event);
            var actual = _sut.Process(@event);

            Assert.Null(actual);
        }

        [Fact]
        public void Process_FirstEventWithoutException_ReturnsEvent()
        {
            var expected = new SentryEvent();

            var actual = _sut.Process(expected);

            Assert.Same(expected, actual);
        }

        [Fact]
        public void Process_FirstEventWithException_ReturnsEvent()
        {
            var expected = new SentryEvent
            {
                Exception = new Exception()
            };

            var actual = _sut.Process(expected);

            Assert.Same(expected, actual);
        }

        [Fact]
        public void Process_SecondEventWithException_ReturnsNull()
        {
            var duplicate = new Exception();
            var first = new SentryEvent
            {
                Exception = duplicate
            };
            var second = new SentryEvent
            {
                Exception = duplicate
            };

            _ = _sut.Process(first);
            var actual = _sut.Process(second);

            Assert.Null(actual);
        }
    }
}
