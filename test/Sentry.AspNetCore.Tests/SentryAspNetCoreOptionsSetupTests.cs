using System;
#if NETCOREAPP2_1 || NET461
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using NSubstitute;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryAspNetCoreOptionsSetupTests
    {
        private readonly SentryAspNetCoreOptionsSetup _sut = new SentryAspNetCoreOptionsSetup(
            Substitute.For<ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider>>(),
            Substitute.For<IWebHostEnvironment>());

        private readonly SentryAspNetCoreOptions _target = new SentryAspNetCoreOptions();

        [Fact]
        public void Filters_KestrelApplicationEvent_NoException_Filtered()
        {
            _sut.Configure(_target);
            Assert.Contains(_target.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical, 13, null));
        }

        [Fact]
        public void Filters_KestrelApplicationEvent_WithException_Filtered()
        {
            _sut.Configure(_target);
            Assert.Contains(_target.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical, 13, new Exception()));
        }

        [Fact]
        public void Filters_KestrelEventId1_WithException_NotFiltered()
        {
            _sut.Configure(_target);
            Assert.DoesNotContain(_target.Filters, f => f.Filter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Trace, 1, null));
        }
    }
}
