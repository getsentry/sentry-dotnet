using log4net.Core;
using Xunit;

namespace Sentry.Log4Net.Tests
{
    public class SentryAppenderTests
    {
        [Fact]
        public void Append_NullEvent_NoOp()
        {
            var sut = new SentryAppender();
            sut.DoAppend(null as LoggingEvent);
        }
    }
}
