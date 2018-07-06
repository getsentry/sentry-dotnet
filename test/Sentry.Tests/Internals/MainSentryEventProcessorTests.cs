using System;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class MainSentryEventProcessorTests
    {
        public SentryOptions SentryOptions { get; set; } = new SentryOptions();
        internal MainSentryEventProcessor Sut { get; set; }

        public MainSentryEventProcessorTests() => Sut = new MainSentryEventProcessor(SentryOptions);

        [Fact]
        public void Process_ReleaseOnOptions_SetToEvent()
        {
            const string expectedVersion = "1.0 - f4d6b23";
            SentryOptions.Release = expectedVersion;
            var evt = new SentryEvent();

            Sut.Process(evt);

            Assert.Equal(expectedVersion, evt.Release);
        }

        [Fact]
        public void Process_NoReleaseOnOptions_SameAsCachedVersion()
        {
            var evt = new SentryEvent();

            Sut.Process(evt);

            Assert.Equal(MainSentryEventProcessor.Release.Value, evt.Release);
        }

        [Fact]
        public void Process_NoLevelOnEvent_SetToError()
        {
            var evt = new SentryEvent
            {
                Level = null
            };

            Sut.Process(evt);

            Assert.Equal(SentryLevel.Error, evt.Level);
        }

        [Fact]
        public void Process_ExceptionProcessors_Invoked()
        {
            var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
            SentryOptions.GetExceptionProcessors = () => new[] { exceptionProcessor };

            var evt = new SentryEvent
            {
                Exception = new Exception()
            };

            Sut.Process(evt);

            exceptionProcessor.Received(1).Process(evt.Exception, evt);
        }

        [Fact]
        public void Process_NoExceptionOnEvent_ExceptionProcessorsNotInvoked()
        {
            var invoked = false;

            SentryOptions.GetExceptionProcessors = () =>
            {
                invoked = true;
                return new[] { Substitute.For<ISentryEventExceptionProcessor>() };
            };

            var evt = new SentryEvent();
            Sut.Process(evt);

            Assert.False(invoked);
        }
    }
}
