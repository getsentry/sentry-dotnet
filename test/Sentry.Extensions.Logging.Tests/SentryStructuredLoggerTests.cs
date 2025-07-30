#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class SentryStructuredLoggerTests : IDisposable
{
    private class Fixture
    {
        public string CategoryName { get; internal set; }
        public IOptions<SentryLoggingOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }
        public SdkVersion Sdk { get; }

        public Queue<SentryLog> CapturedLogs { get; } = new();
        public InMemoryDiagnosticLogger DiagnosticLogger { get; } = new();

        public Fixture()
        {
            var loggingOptions = new SentryLoggingOptions
            {
                Debug = true,
                DiagnosticLogger = DiagnosticLogger,
                Environment = "my-environment",
                Release = "my-release",
            };

            CategoryName = nameof(CategoryName);
            Options = Microsoft.Extensions.Options.Options.Create(loggingOptions);
            Hub = Substitute.For<IHub>();
            Clock = new MockClock(new DateTimeOffset(2025, 04, 22, 14, 51, 00, 789, TimeSpan.FromHours(2)));
            Sdk = new SdkVersion
            {
                Name = "SDK Name",
                Version = "SDK Version",
            };

            var logger = Substitute.For<Sentry.SentryStructuredLogger>();
            logger.CaptureLog(Arg.Do<SentryLog>(log => CapturedLogs.Enqueue(log)));
            Hub.Logger.Returns(logger);

            EnableHub(true);
            EnableLogs(true);
            SetMinimumLogLevel(default);
        }

        public void EnableHub(bool isEnabled) => Hub.IsEnabled.Returns(isEnabled);
        public void EnableLogs(bool isEnabled) => Options.Value.Experimental.EnableLogs = isEnabled;
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
        _fixture.DiagnosticLogger.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(LogLevel.Trace, SentryLogLevel.Trace)]
    [InlineData(LogLevel.Debug, SentryLogLevel.Debug)]
    [InlineData(LogLevel.Information, SentryLogLevel.Info)]
    [InlineData(LogLevel.Warning, SentryLogLevel.Warning)]
    [InlineData(LogLevel.Error, SentryLogLevel.Error)]
    [InlineData(LogLevel.Critical, SentryLogLevel.Fatal)]
    [InlineData(LogLevel.None, default(SentryLogLevel))]
    public void Log_LogLevel_CaptureLog(LogLevel logLevel, SentryLogLevel expectedLevel)
    {
        var traceId = SentryId.Create();
        var parentSpanId = SpanId.Create();
        _fixture.WithTraceHeader(traceId, parentSpanId);
        var logger = _fixture.GetSut();

        EventId eventId = new(123, "EventName");
        Exception? exception = new InvalidOperationException("message");
        string? message = "Message with {Argument}.";
        object?[] args = ["argument"];

        logger.Log(logLevel, eventId, exception, message, args);

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
    public void Log_LogLevelNone_DoesNotCaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.None, new EventId(123, "EventName"), new InvalidOperationException("message"), "Message with {Argument}.", "argument");

        _fixture.CapturedLogs.Should().BeEmpty();
    }

    [Fact]
    public void Log_WithoutTraceHeader_CaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(123, "EventName"), new InvalidOperationException("message"), "Message with {Argument}.", "argument");

        var log = _fixture.CapturedLogs.Dequeue();
        log.TraceId.Should().Be(SentryTraceHeader.Empty.TraceId);
        log.ParentSpanId.Should().Be(SentryTraceHeader.Empty.SpanId);
    }

    [Fact]
    public void Log_WithoutArguments_CaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(123, "EventName"), new InvalidOperationException("message"), "Message.");

        var log = _fixture.CapturedLogs.Dequeue();
        log.Message.Should().Be("Message.");
        log.Template.Should().Be("Message.");
        log.Parameters.Should().BeEmpty();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2017:Parameter count mismatch", Justification = "Tests")]
    public void Log_ParameterCountMismatch_CaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(123, "EventName"), new InvalidOperationException("message"), "Message with {Argument}.");

        var log = _fixture.CapturedLogs.Dequeue();
        log.Message.Should().Be("Message with {Argument}.");
        log.Template.Should().Be("Message with {Argument}.");
        log.Parameters.Should().BeEmpty();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2017:Parameter count mismatch", Justification = "Tests")]
    public void Log_ParameterCountMismatch_Throws()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(123, "EventName"), new InvalidOperationException("message"), "Message with {One}{Two}.", "One");

        _fixture.CapturedLogs.Should().BeEmpty();
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Error);
        entry.Message.Should().Be("Template string does not match the provided argument. The Log will be dropped.");
        entry.Exception.Should().BeOfType<FormatException>();
        entry.Args.Should().BeEmpty();
    }

    [Fact]
    public void Log_WithoutCategoryName_CaptureLog()
    {
        _fixture.CategoryName = null!;
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(123, "EventName"), new InvalidOperationException("message"), "Message with {Argument}.", "argument");

        var log = _fixture.CapturedLogs.Dequeue();
        log.TryGetAttribute("microsoft.extensions.logging.category_name", out object? _).Should().BeFalse();
    }

    [Fact]
    public void Log_WithoutEvent_CaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new InvalidOperationException("message"), "Message with {Argument}.", "argument");

        var log = _fixture.CapturedLogs.Dequeue();
        log.TryGetAttribute("microsoft.extensions.logging.event.id", out object? _).Should().BeFalse();
        log.TryGetAttribute("microsoft.extensions.logging.event.name", out object? _).Should().BeFalse();
    }

    [Fact]
    public void Log_WithoutEventId_CaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(0, "EventName"), new InvalidOperationException("message"), "Message with {Argument}.", "argument");

        var log = _fixture.CapturedLogs.Dequeue();
        log.AssertAttribute("microsoft.extensions.logging.event.id", 0);
        log.AssertAttribute("microsoft.extensions.logging.event.name", "EventName");
    }

    [Fact]
    public void Log_WithoutEventName_CaptureLog()
    {
        var logger = _fixture.GetSut();

        logger.Log(LogLevel.Information, new EventId(123), new InvalidOperationException("message"), "Message with {Argument}.", "argument");

        var log = _fixture.CapturedLogs.Dequeue();
        log.AssertAttribute("microsoft.extensions.logging.event.id", 123);
        log.TryGetAttribute("microsoft.extensions.logging.event.name", out object? _).Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true, LogLevel.Warning, LogLevel.Warning, true)]
    [InlineData(false, true, LogLevel.Warning, LogLevel.Warning, false)]
    [InlineData(true, false, LogLevel.Warning, LogLevel.Warning, false)]
    [InlineData(true, true, LogLevel.Information, LogLevel.Warning, true)]
    [InlineData(true, true, LogLevel.Error, LogLevel.Warning, false)]
    public void IsEnabled_HubOptionsMinimumLogLevel_Returns(bool isHubEnabled, bool isLogsEnabled, LogLevel minimumLogLevel, LogLevel actualLogLevel, bool expectedIsEnabled)
    {
        _fixture.EnableHub(isHubEnabled);
        _fixture.EnableLogs(isLogsEnabled);
        _fixture.SetMinimumLogLevel(minimumLogLevel);
        var logger = _fixture.GetSut();

        var isEnabled = logger.IsEnabled(actualLogLevel);
        logger.Log(actualLogLevel, "message");

        isEnabled.Should().Be(expectedIsEnabled);
        if (expectedIsEnabled)
        {
            _fixture.CapturedLogs.Dequeue().Message.Should().Be("message");
        }
    }

    [Fact]
    public void BeginScope_Dispose_NoOp()
    {
        var logger = _fixture.GetSut();

        string messageFormat = "Message with {Argument}.";
        object?[] args = ["argument"];

        logger.LogInformation("one");
        using (var scope = logger.BeginScope(messageFormat, args))
        {
            logger.LogInformation("two");
        }
        logger.LogInformation("three");

        _fixture.CapturedLogs.Dequeue().Message.Should().Be("one");
        _fixture.CapturedLogs.Dequeue().Message.Should().Be("two");
        _fixture.CapturedLogs.Dequeue().Message.Should().Be("three");
    }

    [Fact]
    public void BeginScope_Shared_Same()
    {
        var logger = _fixture.GetSut();

        using var scope1 = logger.BeginScope("Message with {Argument}.", "argument");
        using var scope2 = logger.BeginScope("Message with {Argument}.", "argument");

        scope1.Should().BeSameAs(scope2);
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
