using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class LoggingOptionsTests
    {
        private readonly LoggingOptions _sut = new LoggingOptions();

        [Fact]
        public void Filters_KestrelApplicationEvent_NoException_Filtered()
        {
            var sut = new LoggingOptions();
            Assert.Contains(sut.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical, 13, null));
        }

        [Fact]
        public void Filters_KestrelApplicationEvent_WithException_Filtered()
        {
            var sut = new LoggingOptions();
            Assert.Contains(sut.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical, 13, new Exception()));
        }

        [Fact]
        public void Filters_KestrelEventId1_WithException_NotFiltered()
        {
            Assert.DoesNotContain(_sut.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Trace, 1, null));
        }

        [Fact]
        public void Ctor_MinimumBreadcrumbLevel_NoLevelSpecifiedByDefault() => Assert.Null(_sut.MinimumBreadcrumbLevel);

        [Fact]
        public void Ctor_MinimumEventLevel_NoLevelSpecifiedByDefault() => Assert.Null(_sut.MinimumEventLevel);
    }
}
