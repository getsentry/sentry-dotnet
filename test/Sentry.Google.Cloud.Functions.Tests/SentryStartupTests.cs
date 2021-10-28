using System;
using System.Collections.Generic;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Sentry.AspNetCore;
using Xunit;

namespace Sentry.Google.Cloud.Functions.Tests
{
    public class SentryStartupTests
    {
        public IWebHostEnvironment HostingEnvironment { get; set; } = Substitute.For<IWebHostEnvironment>();
        public WebHostBuilderContext WebHostBuilderContext { get; set; }

        public IApplicationBuilder ApplicationBuilder { get; set; } = Substitute.For<IApplicationBuilder>();

        public ILoggingBuilder LoggingBuilder { get; set; }

        public SentryStartupTests()
        {
            WebHostBuilderContext = new WebHostBuilderContext
            {
                Configuration = Substitute.For<IConfiguration>(),
                HostingEnvironment = HostingEnvironment
            };
            LoggingBuilder = new TestLoggingBuilder();
            LoggingBuilder.Services.AddSingleton(HostingEnvironment);

        }

        private class TestLoggingBuilder : ILoggingBuilder
        {
            public IServiceCollection Services { get; } = new ServiceCollection();
        }

        [Fact]
        public void ConfigureLogging_SentryAspNetCoreOptions_ReleaseOptionsSet()
        {
            var sut = new SentryStartup();
            var scope = new Scope(null);
            Environment.SetEnvironmentVariable("K_REVISION", "1");
            sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

            var provider = LoggingBuilder.Services.BuildServiceProvider();
            var option = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>();

            Assert.Null(option.Value.Release);

            foreach (var action in option.Value.ConfigureScopeCallbacks)
            {
                action.Invoke(scope);
            }

            Assert.Equal("testhost@16.11.0+1", option.Value.Release);
        }

        [Fact]
        public void Configure_TracingMiddlewaresRegistered()
        {
            // Arrange
            var sut = new SentryStartup();
            var ApplicationBuilder = Substitute.For<IApplicationBuilder>();

            // Act
            sut.Configure(WebHostBuilderContext, ApplicationBuilder);

            // Assert
            // AspNetCore and GCP MiddleWare
            ApplicationBuilder.Received(2).Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
        }

        [Fact]
        public void ConfigureLogging_SentryAspNetCoreOptions_FlushOnCompletedRequestTrue()
        {
            var sut = new SentryStartup();
            sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

            var provider = LoggingBuilder.Services.BuildServiceProvider();
            var option = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>();
            Assert.True(option.Value.FlushBeforeRequestCompleted);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
        {
            var sut = new SentryStartup();
            sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

            var provider = LoggingBuilder.Services.BuildServiceProvider();
            var option = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>();
            assert(LoggingBuilder.Services);
        }

        public static IEnumerable<object[]> ExpectedServices()
        {
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ServiceType == typeof(IHub)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryAspNetCoreLoggerProvider)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryAspNetCoreOptionsSetup)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(AspNetCoreEventProcessor)))};
        }
    }
}
