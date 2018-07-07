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

        // TODO: Test when the approach for parsing is finalized
    }
}
