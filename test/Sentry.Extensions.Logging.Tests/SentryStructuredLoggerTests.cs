#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class SentryStructuredLoggerTests : IDisposable
{
    private class Fixture
    {
        public string CategoryName { get; }
        public IOptions<SentryLoggingOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }
        public SdkVersion Sdk { get; }

        public Queue<SentryLog> CapturedLogs { get; } = new();

        public Fixture()
        {
            var loggingOptions = new SentryLoggingOptions();
            loggingOptions.Experimental.EnableLogs = true;
            loggingOptions.Environment = "my-environment";
            loggingOptions.Release = "my-release";

            CategoryName = nameof(CategoryName);
            Options = Microsoft.Extensions.Options.Options.Create(loggingOptions);
            Hub = Substitute.For<IHub>();
            Clock = new MockClock(new DateTimeOffset(2025, 04, 22, 14, 51, 00, TimeSpan.Zero));
            Sdk = new SdkVersion
            {
                Name = "SDK Name",
                Version = "SDK Version",
            };

            var logger = Substitute.For<Sentry.SentryStructuredLogger>();
            logger.CaptureLog(Arg.Do<SentryLog>(log => CapturedLogs.Enqueue(log)));
            Hub.Logger.Returns(logger);
        }

        public void EnableHub() => Hub.IsEnabled.Returns(true);
        public void DisableHub() => Hub.IsEnabled.Returns(false);

        public void EnableLogs() => Options.Value.Experimental.EnableLogs = true;
        public void DisableLogs() => Options.Value.Experimental.EnableLogs = true;

        public void SetMinimumLogLevel(LogLevel logLevel) => Options.Value.ExperimentalLogging.MinimumLogLevel = logLevel;

        public void WithTraceHeader(SentryId traceId, SpanId parentSpanId)
        {
            var traceHeader = new SentryTraceHeader(traceId, parentSpanId, null);
            Hub.GetTraceHeader().Returns(traceHeader);
        }

        public SentryStructuredLogger GetSut()
        {
            return new SentryStructuredLogger(CategoryName, Options.Value, Hub, Clock, Sdk);
        }
    }

    private readonly Fixture _fixture = new();

    public void Dispose()
    {
        _fixture.CapturedLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(LogLevel.Trace, SentryLogLevel.Trace)]
    [InlineData(LogLevel.Debug, SentryLogLevel.Debug)]
    [InlineData(LogLevel.Information, SentryLogLevel.Info)]
    [InlineData(LogLevel.Warning, SentryLogLevel.Warning)]
    [InlineData(LogLevel.Error, SentryLogLevel.Error)]
    [InlineData(LogLevel.Critical, SentryLogLevel.Fatal)]
    [InlineData(LogLevel.None, default(SentryLogLevel))]
    public void Log_LogLevel_(LogLevel logLevel, SentryLogLevel expectedLevel)
    {
        var traceId = SentryId.Create();
        var parentSpanId = SpanId.Create();

        _fixture.EnableHub();
        _fixture.EnableLogs();
        _fixture.SetMinimumLogLevel(logLevel);
        _fixture.WithTraceHeader(traceId, parentSpanId);
        var logger = _fixture.GetSut();

        EventId eventId = new(123, "EventName");
        Exception? exception = new InvalidOperationException("message");
        string? message = "Message with {Argument}.";

        logger.Log(logLevel, eventId, exception, message, "argument");

        if (logLevel == LogLevel.None)
        {
            _fixture.CapturedLogs.Should().BeEmpty();
            return;
        }

        var log = _fixture.CapturedLogs.Dequeue();
        log.Timestamp.Should().Be(_fixture.Clock.GetUtcNow());
        log.TraceId.Should().Be(traceId);
        log.Level.Should().Be(expectedLevel);
        log.Message.Should().Be("Message with argument.");
        log.Template.Should().Be(message);
        log.Parameters.Should().BeEquivalentTo(new KeyValuePair<string, object>[] { new("Argument", "argument") });
        log.ParentSpanId.Should().Be(parentSpanId);
        log.AssertAttribute("sentry.environment", "my-environment");
        log.AssertAttribute("sentry.release", "my-release");
        log.AssertAttribute("sentry.sdk.name", "SDK Name");
        log.AssertAttribute("sentry.sdk.version", "SDK Version");
        log.AssertAttribute("microsoft.extensions.logging.category_name", "CategoryName");
        log.AssertAttribute("microsoft.extensions.logging.event.id", 123);
        log.AssertAttribute("microsoft.extensions.logging.event.name", "EventName");
    }

    [Fact]
    public void IsEnabled_()
    {
        var logger = _fixture.GetSut();
    }

    [Fact]
    public void BeginScope_()
    {
        var logger = _fixture.GetSut();
    }
}

file static class SentryLogExtensions
{
    public static void AssertAttribute(this SentryLog log, string key, string value)
    {
        log.TryGetAttribute(key, out object? attribute).Should().BeTrue();
        var actual = attribute.Should().BeOfType<string>().Which;
        actual.Should().Be(value);
    }

    public static void AssertAttribute(this SentryLog log, string key, int value)
    {
        log.TryGetAttribute(key, out object? attribute).Should().BeTrue();
        var actual = attribute.Should().BeOfType<int>().Which;
        actual.Should().Be(value);
    }
}
