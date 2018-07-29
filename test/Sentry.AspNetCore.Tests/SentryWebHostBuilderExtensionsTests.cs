using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sentry.Extensions.Logging;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryWebHostBuilderExtensionsTests
    {
        public IWebHostBuilder WebHostBuilder { get; set; } = Substitute.For<IWebHostBuilder>();
        public ServiceCollection Services { get; set; } = new ServiceCollection();
        public IConfiguration Configuration { get; set; } = Substitute.For<IConfiguration>();
        public IHostingEnvironment HostingEnvironment { get; set; } = Substitute.For<IHostingEnvironment>();

        public SentryWebHostBuilderExtensionsTests()
        {
            var context = new WebHostBuilderContext
            {
                Configuration = Configuration,
                HostingEnvironment = HostingEnvironment
            };

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<IServiceCollection>>()))
                .Do(i => i.Arg<Action<IServiceCollection>>()(Services));

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<WebHostBuilderContext, IServiceCollection>>()))
                .Do(i => i.Arg<Action<WebHostBuilderContext, IServiceCollection>>()(context, Services));
        }


        [Fact]
        public void UseSentry_OnInit_RegistersSentryAspNetCoreOptions()
        {
            WebHostBuilder.UseSentry();

            Assert.Single(Services, s => s.ImplementationInstance?.GetType() == typeof(SentryAspNetCoreOptions));
        }

        [Fact]
        public void UseSentry_OnInit_SetsSentryOptionsToAspNetCore()
        {
            WebHostBuilder.UseSentry();

            var aspnetOptions = (SentryAspNetCoreOptions)Services.Single(s => s.ImplementationInstance?.GetType() == typeof(SentryAspNetCoreOptions)).ImplementationInstance;

            var sentryOptions = new SentryOptions();
            aspnetOptions.ConfigureOptionsActions.ForEach(a => a(sentryOptions));

            Assert.Same(sentryOptions, aspnetOptions.SentryOptions);
        }

        [Fact]
        public void UseSentry_OnInit_InvokesCallback()
        {
            var callbackInvoked = false;
            WebHostBuilder.UseSentry(_ => callbackInvoked = true);

            var aspnetOptions = (SentryAspNetCoreOptions)Services.Single(s => s.ImplementationInstance?.GetType() == typeof(SentryAspNetCoreOptions)).ImplementationInstance;

            var sentryOptions = new SentryOptions();
            aspnetOptions.ConfigureOptionsActions.ForEach(a => a(sentryOptions));

            Assert.True(callbackInvoked);
        }

        [Fact]
        public void UseSentry_OnInit_UserCallbackDataNotOverwritten()
        {
            HostingEnvironment.EnvironmentName.Returns("framework defined env");
            const string expectedEnvironment = "environment";
            WebHostBuilder.UseSentry(o => o.Environment = expectedEnvironment);

            var aspnetOptions = (SentryAspNetCoreOptions)Services.Single(s => s.ImplementationInstance?.GetType() == typeof(SentryAspNetCoreOptions)).ImplementationInstance;

            var sentryOptions = new SentryOptions();
            aspnetOptions.ConfigureOptionsActions.ForEach(a => a(sentryOptions));

            Assert.Equal(expectedEnvironment, sentryOptions.Environment);
        }

        [Fact]
        public void UseSentry_OnInit_EnvironmentSetFromAspNetCore()
        {
            const string expectedEnvironment = "environment";
            HostingEnvironment.EnvironmentName.Returns(expectedEnvironment);
            WebHostBuilder.UseSentry();

            var aspnetOptions = (SentryAspNetCoreOptions)Services.Single(s => s.ImplementationInstance?.GetType() == typeof(SentryAspNetCoreOptions)).ImplementationInstance;

            var sentryOptions = new SentryOptions();
            aspnetOptions.ConfigureOptionsActions.ForEach(a => a(sentryOptions));

            Assert.Equal(expectedEnvironment, sentryOptions.Environment);
        }

        [Fact]
        public void UseSentry_OnInit_SetReleaseFromAspNetOptions()
        {
            const string expectedRelease = "release";
            WebHostBuilder.UseSentry(o => o.Release = expectedRelease);

            var aspnetOptions = (SentryAspNetCoreOptions)Services.Single(s => s.ImplementationInstance?.GetType() == typeof(SentryAspNetCoreOptions)).ImplementationInstance;

            var sentryOptions = new SentryOptions();
            aspnetOptions.ConfigureOptionsActions.ForEach(a => a(sentryOptions));

            Assert.Equal(expectedRelease, sentryOptions.Release);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry(DsnSamples.ValidDsnWithoutSecret);
            assert(Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry();
            assert(Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry(Protocol.Constants.DisableSdkDsnValue);
            assert(Services);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry(o => o.InitializeSdk = false);
            assert(Services);
        }

        public static IEnumerable<object[]> ExpectedServices()
        {
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ServiceType == typeof(IHub)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryLoggerProvider)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ServiceType == typeof(SentryAspNetCoreOptions)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryStartupFilter)))};
        }
    }
}
