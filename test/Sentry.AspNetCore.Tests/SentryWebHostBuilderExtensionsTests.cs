using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sentry.Extensions.Logging;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryWebHostBuilderExtensionsTests
    {
        public IWebHostBuilder WebHostBuilder { get; set; } = Substitute.For<IWebHostBuilder>();
        public ServiceCollection Services { get; set; } = new ServiceCollection();
        public IConfiguration Configuration { get; set; } = Substitute.For<IConfiguration>();

        public SentryWebHostBuilderExtensionsTests()
        {
            var context = new WebHostBuilderContext { Configuration = Configuration };

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<IServiceCollection>>()))
                .Do(i => i.Arg<Action<IServiceCollection>>()(Services));

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<WebHostBuilderContext, IServiceCollection>>()))
                .Do(i => i.Arg<Action<WebHostBuilderContext, IServiceCollection>>()(context, Services));
        }

        [Fact]
        public void UseSentry_EnvironmentOnAspNetOptions_SetToSentryOptions()
        {
            const string expected = "test";
            WebHostBuilder.UseSentry(o => o.Environment = expected);
            var options = (SentryAspNetCoreOptions)Services.Single(s => s.ServiceType == typeof(SentryAspNetCoreOptions))
                .ImplementationInstance;

            var target = new SentryOptions();
            options.ConfigureOptionsActions.ForEach(c => c(target));

            Assert.Equal(expected, target.Environment);
        }

        [Fact]
        public void UseSentry_EnvironmentOnEnvVar_SetToSentryOptions()
        {
            const string expected = "test";
            var target = new SentryOptions();

            EnvironmentVariableGuard.WithVariable("ASPNETCORE_ENVIRONMENT",
                expected,
                () =>
                {
                    WebHostBuilder.UseSentry();
                    var options = (SentryAspNetCoreOptions)Services.Single(s => s.ServiceType == typeof(SentryAspNetCoreOptions))
                        .ImplementationInstance;
                    options.ConfigureOptionsActions.ForEach(c => c(target));
                });

            Assert.Equal(expected, target.Environment);
        }

        [Fact]
        public void UseSentry_EnvironmentOnOptionsAndEnvVar_ValueFromOptionsSetToSentryOptions()
        {
            const string expected = "test";
            const string @else = "something else";
            var target = new SentryOptions();

            EnvironmentVariableGuard.WithVariable("ASPNETCORE_ENVIRONMENT",
                @else,
                () =>
                {
                    WebHostBuilder.UseSentry(o => o.Environment = expected);
                    var options = (SentryAspNetCoreOptions)Services.Single(s => s.ServiceType == typeof(SentryAspNetCoreOptions))
                        .ImplementationInstance;
                    options.ConfigureOptionsActions.ForEach(c => c(target));
                });

            Assert.Equal(expected, target.Environment);
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
            WebHostBuilder.UseSentry(Internal.Constants.DisableSdkDsnValue);
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
