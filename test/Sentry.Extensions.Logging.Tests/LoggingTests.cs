using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class LoggingTests
{
    private static readonly string CategoryName = "test_category";

    [SkippableTheory]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void Log_CapturesEvent(LogLevel logLevel)
    {
#if __IOS__
        Skip.If(true, "Flaky on iOS");
#endif
        Skip.If(TestEnvironment.IsGitHubActions, "Flaky on CI");

        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry(o =>
        {
            o.Dsn = ValidDsn;
            o.MinimumBreadcrumbLevel = LogLevel.None;
            o.MinimumEventLevel = logLevel;
            o.BackgroundWorker = worker;
            o.InitNativeSdks = false;
            o.EnableLogs = false;
        }));
        serviceCollection.Configure<LoggerFilterOptions>(options => options.AddFilter<SentryStructuredLoggerProvider>(CategoryName, LogLevel.None));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        const string message = "test message";
        var logger = loggerFactory.CreateLogger(CategoryName);
        logger.Log(logLevel, message);

        // Assert
        worker.Received(1).EnqueueEnvelope(
            Arg.Is<Envelope>(e =>
                e.Items
                    .Select(i => i.Payload).OfType<JsonSerializable>()
                    .Select(i => i.Source).OfType<SentryEvent>()
                    .SingleOrDefault(evt =>
                        evt.Level == logLevel.ToSentryLevel() &&
                        evt.Message.Message == message)
                != null));
    }

    [SkippableTheory]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void Log_AddsBreadcrumb(LogLevel logLevel)
    {
#if __IOS__
        Skip.If(true, "Flaky on iOS");
#endif

        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry(o =>
        {
            o.Dsn = ValidDsn;
            o.MinimumBreadcrumbLevel = logLevel;
            o.MinimumEventLevel = LogLevel.None;
            o.BackgroundWorker = worker;
            o.InitNativeSdks = false;
            o.EnableLogs = false;
        }));
        serviceCollection.Configure<LoggerFilterOptions>(options => options.AddFilter<SentryStructuredLoggerProvider>(CategoryName, LogLevel.None));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        const string message = "test message";
        var logger = loggerFactory.CreateLogger(CategoryName);
        logger.Log(logLevel, message);

        var hub = serviceProvider.GetRequiredService<IHub>();
        hub.CaptureEvent(new SentryEvent());

        // Assert
        worker.Received(1).EnqueueEnvelope(
            Arg.Is<Envelope>(e =>
                e.Items
                    .Select(i => i.Payload).OfType<JsonSerializable>()
                    .Select(i => i.Source).OfType<SentryEvent>()
                    .Single()
                    .Breadcrumbs
                    .SingleOrDefault(b =>
                        b.Level == logLevel.ToBreadcrumbLevel() &&
                        b.Message == message)
                != null));
    }

    [SkippableTheory]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void Log_CapturesStructuredLog(LogLevel logLevel)
    {
#if __IOS__
        Skip.If(true, "Flaky on iOS");
#endif

        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry(o =>
        {
            o.Dsn = ValidDsn;
            o.MinimumBreadcrumbLevel = LogLevel.None;
            o.MinimumEventLevel = LogLevel.None;
            o.BackgroundWorker = worker;
            o.InitNativeSdks = false;
            o.EnableLogs = true;
        }));
        serviceCollection.Configure<LoggerFilterOptions>(options => options.AddFilter<SentryStructuredLoggerProvider>(CategoryName, logLevel));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        const string message = "test message";
        var logger = loggerFactory.CreateLogger(CategoryName);
        logger.Log(logLevel, message);

        var hub = serviceProvider.GetRequiredService<IHub>();
        hub.Logger.Flush();

        // Assert
        worker.Received(1).EnqueueEnvelope(
            Arg.Is<Envelope>(e =>
                e.Items
                    .Select(i => i.Payload).OfType<JsonSerializable>()
                    .Select(i => i.Source).OfType<StructuredLog>()
                    .SingleOrDefault(log => log.Length == 1)
                != null));
    }
}
