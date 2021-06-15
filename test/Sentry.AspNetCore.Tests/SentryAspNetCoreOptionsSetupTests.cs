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
using Sentry.Testing;

namespace Sentry.AspNetCore.Tests
{
    public class SentryAspNetCoreOptionsSetupTests
    {
        private readonly SentryAspNetCoreOptionsSetup _sut = new(
            Substitute.For<ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider>>(),
            Substitute.For<IHostingEnvironment>());

        private readonly SentryAspNetCoreOptions _target = new();

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

        [Theory]
        [InlineData("foo", "Production", "foo")] // Custom - set. Rest ignored.
        [InlineData("Production", "Production", "Production")] // Custom - set. Rest ignored. NOTE: Custom value _happens_ to be the common ASPNET_ENVIRONMENT PROD setting.
        [InlineData(null, "Production", "production")] // Custom - nothing set. ASPNET_ENVIRONMENT is default PROD.
        [InlineData("", "Production", "production")] // Custom - nothing set. ASPNET_ENVIRONMENT is default PROD.
        [InlineData(null, "Development", "development")] // Custom - nothing set. ASPNET_ENVIRONMENT is default DEV.
        [InlineData("", "Development", "development")] // Custom - nothing set. ASPNET_ENVIRONMENT is default DEV.
        [InlineData(null, "Staging", "staging")] // Custom - nothing set. ASPNET_ENVIRONMENT is default STAGING.
        [InlineData("", "Staging", "staging")] // Custom - nothing set. ASPNET_ENVIRONMENT is default STAGING.
        [InlineData(null, "production", "production")] // Custom - nothing set. ASPNET_ENVIRONMENT is custom (notice lowercase 'p').
        [InlineData(null, "development", "development")] // Custom - nothing set. ASPNET_ENVIRONMENT is custom (notice lowercase 'd').
        [InlineData(null, "staging", "staging")] // Custom - nothing set. ASPNET_ENVIRONMENT is custom (notice lowercase 's').
        public void Filters_Environment_CustomOrASPNETEnvironment_Set(string environment, string hostingEnvironmentSetting, string expectedEnvironment)
        {
            // Arrange.
            var hostingEnvironment = Substitute.For<IHostingEnvironment>();
            hostingEnvironment.EnvironmentName = hostingEnvironmentSetting;

            var sut = new SentryAspNetCoreOptionsSetup(
                Substitute.For<ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider>>(),
                hostingEnvironment);

            //const string environment = "some environment";
            _target.Environment = environment;

            // Act.
            sut.Configure(_target);

            // Assert.
            Assert.Equal(expectedEnvironment, _target.Environment);
        }

        [Theory]
        [InlineData("Foo", "Production", true, "Foo")] // Custom - set. Don't modify.
        [InlineData(null, "Foo", true, "Foo")] // Custom - nothing set. ASPNET_ENVIRONMENT is custom. Don't modify.
        [InlineData(null, "Production", true, "production")] // Custom - nothing set. Adjust standard casing - true. ASPNET_ENVIRONMENT is default PROD.
        [InlineData(null, "Development", true, "development")] // Custom - nothing set. Adjust standard casing - true. ASPNET_ENVIRONMENT is default DEV.
        [InlineData(null, "Staging", true, "staging")] // Custom - nothing set. Adjust standard casing - true. ASPNET_ENVIRONMENT is default STAGING.
        [InlineData(null, "Production", false, "Production")] // Custom - nothing set. Adjust standard casing - false. ASPNET_ENVIRONMENT is default PROD.
        [InlineData(null, "Development", false, "Development")] // Custom - nothing set. Adjust standard casing - false. ASPNET_ENVIRONMENT is default DEV.
        [InlineData(null, "Staging", false, "Staging")] // Custom - nothing set. Adjust standard casing - false. ASPNET_ENVIRONMENT is default STAGING.
        public void Filters_Environment_AdjustStandardEnvironmentNameCasing_AffectsSentryEnvironment(string environment, string hostingEnvironmentSetting, bool adjustStandardEnvironmentNameCasingSetting, string expectedEnvironment)
        {
            // Arrange.
            var hostingEnvironment = Substitute.For<IHostingEnvironment>();
            hostingEnvironment.EnvironmentName = hostingEnvironmentSetting;

            var sut = new SentryAspNetCoreOptionsSetup(
                Substitute.For<ILoggerProviderConfiguration<SentryAspNetCoreLoggerProvider>>(),
                hostingEnvironment);

            _target.Environment = environment;
            _target.AdjustStandardEnvironmentNameCasing = adjustStandardEnvironmentNameCasingSetting;

            // Act.
            sut.Configure(_target);

            // Assert.
            Assert.Equal(expectedEnvironment, _target.Environment);
        }

        [Theory]
        [InlineData("foo")] // Random setting.
        [InlineData("Production")] // Custom setting which is the same as ASPNET_ENVIRONMENT. But because this is manually set, don't change it.
        public void Filters_Environment_SentryEnvironment_Set(string environment)
        {
            // Arrange.
            EnvironmentVariableGuard.WithVariable(Internal.Constants.EnvironmentEnvironmentVariable, environment, () =>
            {
                // Act.
                _sut.Configure(_target);

                // Assert.
                Assert.Equal(environment, _target.Environment);
            });
        }
    }
}
