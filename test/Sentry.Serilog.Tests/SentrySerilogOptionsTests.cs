using Serilog.Events;
using Xunit;

namespace Sentry.Serilog.Tests
{
    public class SentrySerilogOptionsTests
    {
        [Fact]
        public void Ctor_MinimumBreadcrumbLevel_Information()
        {
            var sut = new SentrySerilogOptions();
            Assert.Equal(LogEventLevel.Information, sut.MinimumBreadcrumbLevel);
        }

        [Fact]
        public void Ctor_MinimumEventLevel_Error()
        {
            var sut = new SentrySerilogOptions();
            Assert.Equal(LogEventLevel.Error, sut.MinimumEventLevel);
        }
    }
}
