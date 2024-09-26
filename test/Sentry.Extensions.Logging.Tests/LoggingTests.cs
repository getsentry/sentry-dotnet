using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class LoggingTests
{
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

        // Arrange
        var worker = Substitute.For<IBackgroundWorker>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddSentry(o =>
        {
            o.Dsn = ValidDsn;
            o.MinimumEventLevel = logLevel;
            o.MinimumBreadcrumbLevel = LogLevel.None;
            o.BackgroundWorker = worker;
            o.InitNativeSdks = false;
        }));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        const string message = "test message";
        var logger = loggerFactory.CreateLogger("test");
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

    [Theory]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void Log_AddsBreadcrumb(LogLevel logLevel)
    {
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
        }));
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        const string message = "test message";
        var logger = loggerFactory.CreateLogger("test");
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
}
