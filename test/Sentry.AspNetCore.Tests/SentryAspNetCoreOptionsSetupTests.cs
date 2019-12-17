using System;
#if NETCOREAPP2_1 || NET461
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
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
            Substitute.For<IHostingEnvironment>());

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
