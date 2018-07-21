using System;
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
            Assert.Null(evt.SentryExceptions);
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

            Assert.Equal($"System.Exception.Data[{expectedKey}]", actual.Key);
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

        // TODO: Test when the approach for parsing is finalized
    }
}
