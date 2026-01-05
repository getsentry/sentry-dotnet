using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;

namespace Sentry.Google.Cloud.Functions.Tests;

public class SentryStartupTests
{
    public IWebHostEnvironment HostingEnvironment { get; set; } = Substitute.For<IWebHostEnvironment>();
    public WebHostBuilderContext WebHostBuilderContext { get; set; }

    public ILoggingBuilder LoggingBuilder { get; set; }

    public SentryStartupTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        WebHostBuilderContext = new WebHostBuilderContext
        {
            Configuration = configuration,
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
    public void ConfigureLogging_ReadsKRevisionEnvVar_AppendsToRelease()
    {
        LoggingBuilder.Services.Configure<SentryAspNetCoreOptions>(o =>
            o.FakeSettings().EnvironmentVariables["K_REVISION"] = "9");

        var sut = new SentryStartup();
        sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

        var provider = LoggingBuilder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
        Assert.EndsWith("+9", options.Release);
    }

    [Fact]
    public void ConfigureLogging_IgnoresKRevisionEnvVar_WhenReleaseAlreadySet()
    {
        LoggingBuilder.Services.Configure<SentryAspNetCoreOptions>(o =>
        {
            o.Release = "Foo";
            o.FakeSettings().EnvironmentVariables["K_REVISION"] = "9";
        });

        var sut = new SentryStartup();
        sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

        var provider = LoggingBuilder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
        Assert.Equal("Foo", options.Release);
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
    public void ConfigureLogging_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
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

    [Fact]
    public void ConfigureLogging_Logging_AddLoggerProviders()
    {
        LoggingBuilder.Services.Configure<SentryAspNetCoreOptions>(options =>
        {
            options.EnableLogs = true;
            options.InitializeSdk = false;
        });

        var sut = new SentryStartup();
        sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

        using var serviceProvider = LoggingBuilder.Services.BuildServiceProvider();
        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>().ToArray();

        providers.Should().HaveCount(2);
        providers[0].Should().BeOfType<SentryAspNetCoreLoggerProvider>();
        providers[1].Should().BeOfType<SentryAspNetCoreStructuredLoggerProvider>();
    }

    [Fact]
    public void ConfigureLogging_Logging_AddLoggerFilterRules()
    {
        LoggingBuilder.Services.Configure<SentryAspNetCoreOptions>(options =>
        {
            options.EnableLogs = true;
            options.InitializeSdk = false;
        });

        var sut = new SentryStartup();
        sut.ConfigureLogging(WebHostBuilderContext, LoggingBuilder);

        using var serviceProvider = LoggingBuilder.Services.BuildServiceProvider();
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
