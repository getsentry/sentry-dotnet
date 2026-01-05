using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

#if NETCOREAPP3_1_OR_GREATER
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Sentry.AspNetCore.Tests;

public class SentryWebHostBuilderExtensionsTests
{
    public IWebHostBuilder WebHostBuilder { get; set; } = Substitute.For<IWebHostBuilder>();
    public ServiceCollection Services { get; set; } = new();
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

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry(ValidDsn);
        assert(Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry();
        assert(Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry(Sentry.SentryConstants.DisableSdkDsnValue);
        assert(Services);
    }

    [Theory, MemberData(nameof(ExpectedServices))]
    public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
    {
        _ = WebHostBuilder.UseSentry(o => o.InitializeSdk = false);
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

    [Fact]
    public void UseSentry_Logging_AddLoggerProviders()
    {
#if NET8_0
        var section = Substitute.For<IConfigurationSection>();
        section[Arg.Any<string>()].Returns((string)null);
        Configuration.GetSection("Sentry").Returns(section);
#endif
        WebHostBuilder.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.EnableLogs = true;
            options.InitializeSdk = false;
        });
        using var serviceProvider = Services.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>().ToArray();

        providers.Should().HaveCount(2);
        providers[0].Should().BeOfType<SentryAspNetCoreLoggerProvider>();
        providers[1].Should().BeOfType<SentryAspNetCoreStructuredLoggerProvider>();
    }

    [Fact]
    public void UseSentry_Logging_AddLoggerFilterRules()
    {
        WebHostBuilder.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.EnableLogs = true;
            options.InitializeSdk = false;
        });
        using var serviceProvider = Services.BuildServiceProvider();

        var loggerFilterOptions = serviceProvider.GetRequiredService<IOptions<LoggerFilterOptions>>().Value;

        loggerFilterOptions.Rules.Should().HaveCount(3);
        var one = loggerFilterOptions.Rules[0];
        var two = loggerFilterOptions.Rules[1];
        var three = loggerFilterOptions.Rules[2];

        one.ProviderName.Should().Be(typeof(SentryAspNetCoreLoggerProvider).FullName);
        one.CategoryName.Should().BeNull();
        one.LogLevel.Should().BeNull();
        one.Filter.Should().NotBeNull();
        one.Filter!.Invoke(null, null, LogLevel.None).Should().BeTrue();
        one.Filter.Invoke("", "", LogLevel.None).Should().BeTrue();
        one.Filter.Invoke("type", "category", LogLevel.None).Should().BeTrue();
        one.Filter.Invoke(null, typeof(Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware).FullName, LogLevel.None).Should().BeFalse();

        two.ProviderName.Should().Be(typeof(SentryAspNetCoreStructuredLoggerProvider).FullName);
        two.CategoryName.Should().Be(typeof(ISentryClient).FullName);
        two.LogLevel.Should().Be(LogLevel.None);
        two.Filter.Should().BeNull();

        three.ProviderName.Should().Be(typeof(SentryAspNetCoreStructuredLoggerProvider).FullName);
        three.CategoryName.Should().Be(typeof(SentryMiddleware).FullName);
        three.LogLevel.Should().Be(LogLevel.None);
        three.Filter.Should().BeNull();
    }
}
