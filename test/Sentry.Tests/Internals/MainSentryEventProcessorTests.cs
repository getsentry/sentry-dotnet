using Sentry.Internal;
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
    }
}
