using System;
using System.Collections.Generic;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Hosting;
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
        public void ConfigureLogging_SentryAspNetCoreOptions_FlushOnCompletedRequestTrue()
        {
            var sut = new SentryStartup();
            sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

            var provider = LoggingBuilder.Services.BuildServiceProvider();
            var option = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>();
            Assert.True(option.Value.FlushOnCompletedRequest);
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
