using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class LoggingBuilderExtensionsTests
{
    [Fact]
    public void AddSentry_LoggingBuilder_AddLoggerProviders()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging((ILoggingBuilder builder) => builder.AddSentry(options =>
        {
            options.EnableLogs = true;
            options.InitializeSdk = false;
        }));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>().ToArray();

        // Assert
        providers.Should().HaveCount(2);
        providers[0].Should().BeOfType<SentryLoggerProvider>();
        providers[1].Should().BeOfType<SentryStructuredLoggerProvider>();
    }

    [Fact]
    public void AddSentry_LoggingBuilder_AddLoggerFilterRules()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging((ILoggingBuilder builder) => builder.AddSentry(options =>
        {
            options.EnableLogs = true;
            options.InitializeSdk = false;
        }));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        var loggerFilterOptions = serviceProvider.GetRequiredService<IOptions<LoggerFilterOptions>>().Value;

        // Assert
        loggerFilterOptions.Rules.Should().HaveCount(2);
        var one = loggerFilterOptions.Rules[0];
        var two = loggerFilterOptions.Rules[1];

        one.ProviderName.Should().Be(typeof(SentryLoggerProvider).FullName);
        one.CategoryName.Should().BeNull();
        one.LogLevel.Should().BeNull();
        one.Filter.Should().NotBeNull();
        one.Filter!.Invoke(null, null, LogLevel.None).Should().BeTrue();
        one.Filter.Invoke("", "", LogLevel.None).Should().BeTrue();
        one.Filter.Invoke("type", "category", LogLevel.None).Should().BeTrue();

        two.ProviderName.Should().Be(typeof(SentryStructuredLoggerProvider).FullName);
        two.CategoryName.Should().Be(typeof(ISentryClient).FullName);
        two.LogLevel.Should().Be(LogLevel.None);
        two.Filter.Should().BeNull();
    }

    [Fact]
    public void AddSentry_DerivedOptions_LoggerUsesLogEntryFilters()
    {
        // Arrange
        var hub = CreateHub();
        using var serviceProvider = CreateServiceProvider(hub,
            options => options.AddLogEntryFilter((category, _, _, _) => category == "Filtered"));
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        loggerFactory.CreateLogger("Filtered").LogError("filtered");
        loggerFactory.CreateLogger("Allowed").LogError("allowed");

        // Assert
        _ = hub.DidNotReceive().CaptureEvent(Arg.Is<SentryEvent>(e => e.Logger == "Filtered"));
        _ = hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Logger == "Allowed"));
    }

    [Fact]
    public void AddSentry_DerivedOptions_StructuredLoggerUsesEnableLogs()
    {
        // Arrange
        var hub = CreateHub();
        SentryLog capturedLog = null;
        hub.Logger.CaptureLog(Arg.Do<SentryLog>(log => capturedLog = log));
        using var serviceProvider = CreateServiceProvider(hub, options => options.EnableLogs = true);
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("App");

        // Act
        logger.LogInformation("message");

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog.Message.Should().Be("message");
    }

    private class DerivedSentryLoggingOptions : SentryLoggingOptions;

    private static IHub CreateHub()
    {
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        hub.Logger.Returns(Substitute.For<Sentry.SentryStructuredLogger>());
        return hub;
    }

    private static ServiceProvider CreateServiceProvider(IHub hub, Action<DerivedSentryLoggingOptions> configureOptions)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(hub);
        serviceCollection.AddLogging(builder => builder.AddSentry<DerivedSentryLoggingOptions>(options =>
        {
            options.InitializeSdk = false;
            configureOptions(options);
        }));
        return serviceCollection.BuildServiceProvider();
    }
}
