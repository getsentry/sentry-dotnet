using NLog;
using Xunit;

namespace Sentry.NLog.Tests
{
    public class SentryNLogOptionsTests
    {
        [Fact]
        public void Ctor_MinimumBreadcrumbLevel_Information()
        {
            var options = new SentryNLogOptions();
            Assert.Equal(LogLevel.Info, options.MinimumBreadcrumbLevel);
        }

        [Fact]
        public void Ctor_MinimumEventLevel_Error()
        {
            var options = new SentryNLogOptions();
            Assert.Equal(LogLevel.Error, options.MinimumEventLevel);
        }
    }
}
