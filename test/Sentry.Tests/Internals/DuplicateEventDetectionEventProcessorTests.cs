using System;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class DuplicateEventDetectionEventProcessorTests
    {
        private class Fixture
        {
            public SentryOptions Options { get; set; } = new();

            public DuplicateEventDetectionEventProcessor GetSut() => new(Options);
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void Process_DuplicateEvent_ReturnsNull()
        {
            var @event = new SentryEvent();
            var sut = _fixture.GetSut();
            _ = sut.Process(@event);
            var actual = sut.Process(@event);

            Assert.Null(actual);
        }

        [Fact]
        public void Process_DuplicateEventDisabled_DoesNotReturnsNull()
        {
            _fixture.Options.DeduplicateMode ^= DeduplicateMode.SameEvent;
            var @event = new SentryEvent();
            var sut = _fixture.GetSut();
            _ = sut.Process(@event);
            var actual = sut.Process(@event);

            Assert.NotNull(actual);
        }

        [Fact]
        public void Process_FirstEventWithoutException_ReturnsEvent()
        {
            var expected = new SentryEvent();

            var sut = _fixture.GetSut();
            var actual = sut.Process(expected);

            Assert.Same(expected, actual);
        }

        [Fact]
        public void Process_FirstEventWithException_ReturnsEvent()
        {
            var expected = new SentryEvent(new Exception());

            var sut = _fixture.GetSut();
            var actual = sut.Process(expected);

            Assert.Same(expected, actual);
        }

        [Fact]
        public void Process_SecondEventWithSameExceptionInstance_ReturnsNull()
        {
            var duplicate = new Exception();
            var first = new SentryEvent(duplicate);
            var second = new SentryEvent(duplicate);

            var sut = _fixture.GetSut();
            _ = sut.Process(first);
            var actual = sut.Process(second);

            Assert.Null(actual);
        }

        [Fact]
        public void Process_SecondEventWithSameExceptionInstanceDisabled_DoesNotReturnsNull()
        {
            _fixture.Options.DeduplicateMode ^= DeduplicateMode.SameExceptionInstance;
            var duplicate = new Exception();
            var first = new SentryEvent(duplicate);
            var second = new SentryEvent(duplicate);

            var sut = _fixture.GetSut();
            _ = sut.Process(first);
            var actual = sut.Process(second);

            Assert.NotNull(actual);
        }

        [Fact]
        public void Process_AggregateExceptionDupe_ReturnsNull()
        {
            var duplicate = new Exception();
            var first = new SentryEvent(new AggregateException(duplicate));
            var second = new SentryEvent(duplicate);

            var sut = _fixture.GetSut();
            _ = sut.Process(first);
            var actual = sut.Process(second);

            Assert.Null(actual);
        }

        [Fact]
        public void Process_AggregateExceptionDupeDisabled_DoesNotReturnsNull()
        {
            _fixture.Options.DeduplicateMode ^= DeduplicateMode.AggregateException;
            var duplicate = new Exception();
            var first = new SentryEvent(new AggregateException(duplicate));
            var second = new SentryEvent(duplicate);

            var sut = _fixture.GetSut();
            _ = sut.Process(first);
            var actual = sut.Process(second);

            Assert.NotNull(actual);
        }

        [Fact]
        public void Process_InnerExceptionHasAggregateExceptionDupe_DoesNotReturnsNullByDefault()
        {
            var duplicate = new Exception();
            var first = new SentryEvent(new InvalidOperationException("test", new AggregateException(duplicate)));
            var second = new SentryEvent(new InvalidOperationException("another test",
                new Exception("testing", new AggregateException(duplicate))));

            var sut = _fixture.GetSut();
            _ = sut.Process(first);
            var actual = sut.Process(second);

            Assert.NotNull(actual);
        }

        [Fact]
        public void Process_InnerExceptionHasAggregateExceptionDupe_ReturnsNull()
        {
            _fixture.Options.DeduplicateMode |= DeduplicateMode.InnerException;

            var duplicate = new Exception();
            var first = new SentryEvent(new InvalidOperationException("test", new AggregateException(duplicate)));
            var second = new SentryEvent(new InvalidOperationException("another test",
                new Exception("testing", new AggregateException(duplicate))));

            var sut = _fixture.GetSut();
            _ = sut.Process(first);
            var actual = sut.Process(second);

            Assert.Null(actual);
        }
    }
}
