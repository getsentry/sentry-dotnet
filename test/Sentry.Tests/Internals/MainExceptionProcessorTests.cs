using System;
using System.Linq;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class MainExceptionProcessorTests
    {
        internal MainExceptionProcessor Sut { get; set; } = new MainExceptionProcessor();

        [Fact]
        public void Process_NullException_NoSentryException()
        {
            var evt = new SentryEvent();
            Sut.Process(null, evt);

            Assert.Null(evt.Exception);
            Assert.Null(evt.SentryExceptionValues);
        }

        [Fact]
        public void Process_ExceptionAndEventWithoutExtra_ExtraIsNull()
        {
            var evt = new SentryEvent();
            Sut.Process(new Exception(), evt);

            Assert.Null(evt.InternalExtra);
        }

        [Fact]
        public void Process_ExceptionsWithoutData_ExtraIsNull()
        {
            var evt = new SentryEvent();
            Sut.Process(new Exception("ex", new Exception()), evt);

            Assert.Null(evt.InternalExtra);
        }

        [Fact]
        public void Process_EventAndExceptionHaveExtra_DataCombined()
        {
            const string expectedKey = "extra";
            const int expectedValue = 1;

            var evt = new SentryEvent();
            evt.SetExtra(expectedKey, expectedValue);

            var ex = new Exception();
            ex.Data.Add("other extra", 2);

            Sut.Process(ex, evt);

            Assert.Equal(2, evt.Extra.Count);
            Assert.Contains(evt.Extra, p => p.Key == expectedKey && (int)p.Value == expectedValue);
        }

        [Fact]
        public void Process_EventHasNoExtrasExceptionDoes_DataInEvent()
        {
            const string expectedKey = "extra";
            const int expectedValue = 1;

            var evt = new SentryEvent();

            var ex = new Exception();
            ex.Data.Add(expectedKey, expectedValue);

            Sut.Process(ex, evt);

            var actual = Assert.Single(evt.Extra);

            Assert.Equal($"Exception[0][{expectedKey}]", actual.Key);
            Assert.Equal(expectedValue, (int)actual.Value);
        }

        [Fact]
        public void Process_EventHasExtrasExceptionDoesnt_NotModified()
        {
            var evt = new SentryEvent();
            evt.SetExtra("extra", 1);
            var expected = evt.Extra;

            var ex = new Exception();

            Sut.Process(ex, evt);

            Assert.Same(expected, evt.Extra);
        }

        [Fact]
        public void Process_TwoExceptionsWithData_DataOnEventExtra()
        {
            var evt = new SentryEvent();

            var first = new Exception();
            var firstValue = new object();
            first.Data.Add("first", firstValue);
            var second = new Exception("second", first);
            var secondValue = new object();
            second.Data.Add("second", secondValue);

            Sut.Process(second, evt);

            Assert.Equal(2, evt.Extra.Count);
            Assert.Contains(evt.Extra, e => e.Key == "Exception[0][first]" && e.Value == firstValue);
            Assert.Contains(evt.Extra, e => e.Key == "Exception[1][second]" && e.Value == secondValue);
        }

        // TODO: Test when the approach for parsing is finalized
    }
}
