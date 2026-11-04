using Microsoft.Extensions.Configuration;
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

    [Fact]
    public void AddSentry_CategoryLogLevelInSentrySection_StillCapturesEventsAndBreadcrumbs()
    {
        // Arrange
        var hub = CreateHub();
        var scope = new Scope(new SentryOptions());
        hub.SubstituteConfigureScope(scope);
        using var serviceProvider = CreateServiceProvider(hub, ("Logging:Sentry:LogLevel:Noisy", "None"));
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Noisy.Thing");

        // Act
        logger.LogInformation("breadcrumb");
        logger.LogError("error");

        // Assert
        _ = hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Logger == "Noisy.Thing"));
        scope.Breadcrumbs.Should().Contain(b => b.Category == "Noisy.Thing" && b.Message == "breadcrumb");
    }

    [Fact]
    public void AddSentry_CategoryLogLevelInSentrySection_FiltersStructuredLogs()
    {
        // Arrange
        var hub = CreateHub();
        var capturedLogs = new List<SentryLog>();
        hub.Logger.CaptureLog(Arg.Do<SentryLog>(capturedLogs.Add));
        using var serviceProvider = CreateServiceProvider(hub, ("Logging:Sentry:LogLevel:Noisy", "Error"));
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        loggerFactory.CreateLogger("Noisy.Thing").LogInformation("noisy");
        loggerFactory.CreateLogger("Noisy.Thing").LogError("noisy error");
        loggerFactory.CreateLogger("App").LogInformation("app");

        // Assert
        capturedLogs.Select(log => log.Message).Should().BeEquivalentTo("noisy error", "app");
    }

    [Fact]
    public void AddSentry_SettingsInSentrySection_BindToOptions()
    {
        // Arrange
        using var serviceProvider = CreateServiceProvider(CreateHub(),
            ("Logging:Sentry:Dsn", ValidDsn),
            ("Logging:Sentry:MinimumEventLevel", "Warning"));

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

        // Assert
        options.Dsn.Should().Be(ValidDsn);
        options.MinimumEventLevel.Should().Be(LogLevel.Warning);
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

    private static ServiceProvider CreateServiceProvider(IHub hub, params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => s.Value))
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(hub);
        serviceCollection.AddLogging(builder => builder
            .AddConfiguration(configuration.GetSection("Logging"))
            .AddSentry(options =>
            {
                options.EnableLogs = true;
                options.InitializeSdk = false;
            }));
        return serviceCollection.BuildServiceProvider();
    }
}
