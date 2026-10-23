using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

[Collection(nameof(SentrySdkCollection))]
public sealed class LoggingBuilderExtensionsTests : IDisposable
{
    public void Dispose() => SentrySdk.Close();

    [Fact]
    public void AddSentry_LoggingBuilder_AddLoggerProviders()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging((ILoggingBuilder builder) => builder.AddSentry());
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
    public void AddSentry_HostLoggerProvidersRegistered_AddsNullLoggerProviders()
    {
        var serviceCollection = new ServiceCollection();
        var events = Substitute.For<ILoggerProvider>();
        var structuredLogs = Substitute.For<ILoggerProvider>();
        HostLoggerProviders.Add(serviceCollection, _ => events, _ => structuredLogs);
        serviceCollection.AddLogging(builder => builder.AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>();

        providers.Should().Equal(events, structuredLogs, NullLoggerProvider.Instance, NullLoggerProvider.Instance);
    }

    [Fact]
    public void AddSentry_HostLoggerProvidersCleared_AddsHostLoggerProvidersBack()
    {
        var serviceCollection = new ServiceCollection();
        var events = Substitute.For<ILoggerProvider>();
        var structuredLogs = Substitute.For<ILoggerProvider>();
        HostLoggerProviders.Add(serviceCollection, _ => events, _ => structuredLogs);
        serviceCollection.AddLogging(builder => builder.ClearProviders().AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>();

        providers.Should().Equal(events, structuredLogs);
    }

    [Fact]
    public void AddSentry_HostLoggerProvidersThatStandDownCleared_AddsBackTheOtherHostLoggerProviders()
    {
        var serviceCollection = new ServiceCollection();
        var events = Substitute.For<ILoggerProvider>();
        var structuredLogs = Substitute.For<ILoggerProvider>();
        HostLoggerProviders.Add(serviceCollection, _ => NullLoggerProvider.Instance, _ => NullLoggerProvider.Instance);
        HostLoggerProviders.Add(serviceCollection, _ => events, _ => structuredLogs);
        serviceCollection.AddLogging(builder => builder.ClearProviders().AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>();

        providers.Should().Equal(events, structuredLogs);
    }

    [Fact]
    public void AddSentry_CalledTwice_AddsLoggerProvidersOnce()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry().AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>();

        providers.Select(provider => provider.GetType()).Should().Equal(
            typeof(SentryLoggerProvider), typeof(SentryStructuredLoggerProvider));
    }

    [Fact]
    public void AddSentry_CalledAgainAfterClearProviders_AddsLoggerProvidersAgain()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry().ClearProviders().AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>();

        providers.Select(provider => provider.GetType()).Should().Equal(
            typeof(SentryLoggerProvider), typeof(SentryStructuredLoggerProvider));
    }

    [Fact]
    public void AddSentry_CalledTwiceAfterHostLoggerProvidersCleared_AddsHostLoggerProvidersBackOnce()
    {
        var serviceCollection = new ServiceCollection();
        var events = Substitute.For<ILoggerProvider>();
        var structuredLogs = Substitute.For<ILoggerProvider>();
        HostLoggerProviders.Add(serviceCollection, _ => events, _ => structuredLogs);
        serviceCollection.AddLogging(builder => builder.ClearProviders().AddSentry().AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var providers = serviceProvider.GetRequiredService<IEnumerable<ILoggerProvider>>();

        providers.Should().Equal(events, structuredLogs);
    }

    [Fact]
    public void AddSentry_CalledTwiceWithOptions_AppliesBothOptions()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder
            .AddSentry(options => options.MinimumEventLevel = LogLevel.Critical)
            .AddSentry(options => options.MinimumBreadcrumbLevel = LogLevel.Warning));
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

        options.MinimumEventLevel.Should().Be(LogLevel.Critical);
        options.MinimumBreadcrumbLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void AddSentry_LoggingBuilder_AddLoggerFilterRules()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging((ILoggingBuilder builder) => builder.AddSentry());
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
    public void AddSentry_LoggingBuilder_RegistersNoHub()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        serviceProvider.GetService<IHub>().Should().BeNull();
        serviceProvider.GetService<ISentryClient>().Should().BeNull();
        serviceProvider.GetService<Func<IHub>>().Should().BeNull();
    }

    [Fact]
    public void AddSentry_SdkReinitializedAfterLoggerCreated_LoggerUsesNewHub()
    {
        var firstWorker = Substitute.For<IBackgroundWorker>();
        var secondWorker = Substitute.For<IBackgroundWorker>();
        InitSdk(firstWorker);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry());
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");

        InitSdk(secondWorker);
        logger.LogError("error");

        firstWorker.DidNotReceive().EnqueueEnvelope(Arg.Any<Envelope>());
        secondWorker.Received(1).EnqueueEnvelope(Arg.Is<Envelope>(e =>
            e.Items
                .Select(i => i.Payload).OfType<JsonSerializable>()
                .Select(i => i.Source).OfType<SentryEvent>()
                .Any(evt => evt.Message!.Message == "error")));
    }

    [Fact]
    public void AddSentry_LoggingBuilder_AddsSentryHttpMessageHandler()
    {
        InitSdk(Substitute.For<IBackgroundWorker>());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry());
        serviceCollection.AddHttpClient("test");
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        client.GetMessageHandlers().OfType<SentryHttpMessageHandler>().Should().ContainSingle();
    }

    private static void InitSdk(IBackgroundWorker worker) => SentrySdk.Init(o =>
    {
        o.Dsn = ValidDsn;
        o.BackgroundWorker = worker;
        o.AutoSessionTracking = false;
        o.InitNativeSdks = false;
    });
}
