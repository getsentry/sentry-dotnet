using System;
using System.Collections.Generic;
#if NETCOREAPP2_1 || NET461
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostEnvironment;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryHostBuilderExtensionsTests
    {
        public IHostBuilder HostBuilder { get; set; } = Substitute.For<IHostBuilder>();
        public ServiceCollection Services { get; set; } = new();
        public IConfiguration Configuration { get; set; } = Substitute.For<IConfiguration>();
        public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();

        public ILoggingBuilder LoggingBuilder { get; set; } = Substitute.For<ILoggingBuilder>();

        public SentryHostBuilderExtensionsTests()
        {
            var context = new HostBuilderContext(new Dictionary<object, object>())
            {
                Configuration = Configuration,
                HostingEnvironment = HostingEnvironment
            };

            //LoggingBuilder.Services.Returns(Services);

            HostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<IServiceCollection>>()))
                .Do(i => i.Arg<Action<IServiceCollection>>()(Services));

            // HostBuilder
            //     .When(b => b.ConfigureLogging(Arg.Any<Action<HostBuilderContext, ILoggingBuilder>>()))
            //     .Do(i => i.Arg<Action<HostBuilderContext, ILoggingBuilder>>()(context, LoggingBuilder));

            HostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>()))
                .Do(i => i.Arg<Action<HostBuilderContext, IServiceCollection>>()(context, Services));
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = HostBuilder.UseSentry(DsnSamples.ValidDsnWithoutSecret);
            assert(Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = HostBuilder.UseSentry();
            assert(Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = HostBuilder.UseSentry(Sentry.Constants.DisableSdkDsnValue);
            assert(Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
        {
            _ = HostBuilder.UseSentry(o => o.InitializeSdk = false);
            assert(Services);
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
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryStartupFilter)))};
        }
    }
}
