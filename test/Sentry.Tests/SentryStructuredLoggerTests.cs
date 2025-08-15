#nullable enable

namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
/// </summary>
public class SentryStructuredLoggerTests : IDisposable
{
    internal sealed class Fixture
    {
        public Fixture()
        {
            DiagnosticLogger = new InMemoryDiagnosticLogger();
            Hub = Substitute.For<IHub>();
            Options = new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = DiagnosticLogger,
            };
            Clock = new MockClock(new DateTimeOffset(2025, 04, 22, 14, 51, 00, 789, TimeSpan.FromHours(2)));
            BatchSize = 2;
            BatchTimeout = Timeout.InfiniteTimeSpan;
            TraceId = SentryId.Create();
            ParentSpanId = SpanId.Create();

            Hub.IsEnabled.Returns(true);

            var traceHeader = new SentryTraceHeader(TraceId, ParentSpanId.Value, null);
            Hub.GetTraceHeader().Returns(traceHeader);
        }

        public InMemoryDiagnosticLogger DiagnosticLogger { get; }
        public IHub Hub { get; }
        public SentryOptions Options { get; }
        public ISystemClock Clock { get; }
        public int BatchSize { get; set; }
        public TimeSpan BatchTimeout { get; set; }
        public SentryId TraceId { get; private set; }
        public SpanId? ParentSpanId { get; private set; }

        public void WithoutTraceHeader()
        {
            Hub.GetTraceHeader().Returns((SentryTraceHeader?)null);
            TraceId = SentryId.Empty;
            ParentSpanId = SpanId.Empty;
        }

        public SentryStructuredLogger GetSut() => SentryStructuredLogger.Create(Hub, Options, Clock, BatchSize, BatchTimeout);
    }

    private readonly Fixture _fixture;

    public SentryStructuredLoggerTests()
    {
        _fixture = new Fixture();
    }

    public void Dispose()
    {
        _fixture.DiagnosticLogger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Create_Enabled_NewDefaultInstance()
    {
        _fixture.Options.Experimental.EnableLogs = true;

        var instance = _fixture.GetSut();
        var other = _fixture.GetSut();

        instance.Should().BeOfType<DefaultSentryStructuredLogger>();
        instance.Should().NotBeSameAs(other);
    }

    [Fact]
    public void Create_Disabled_CachedDisabledInstance()
    {
        _fixture.Options.Experimental.EnableLogs.Should().BeFalse();

        var instance = _fixture.GetSut();
        var other = _fixture.GetSut();

        instance.Should().BeOfType<DisabledSentryStructuredLogger>();
        instance.Should().BeSameAs(other);
    }

    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_Enabled_CapturesEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.AssertEnvelope(_fixture, level);
    }

    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_Disabled_DoesNotCaptureEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetSut();

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void Log_WithoutTraceHeader_CapturesEnvelope()
    {
        _fixture.WithoutTraceHeader();
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.AssertEnvelope(_fixture, SentryLogLevel.Trace);
    }

    [Fact]
    public void Log_WithBeforeSendLog_InvokesCallback()
    {
        var invocations = 0;
        SentryLog configuredLog = null!;

        _fixture.Options.Experimental.EnableLogs = true;
        _fixture.Options.Experimental.SetBeforeSendLog((SentryLog log) =>
        {
            invocations++;
            configuredLog = log;
            return log;
        });
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
        configuredLog.AssertLog(_fixture, SentryLogLevel.Trace);
    }

    [Fact]
    public void Log_WhenBeforeSendLogReturnsNull_DoesNotCaptureEnvelope()
    {
        var invocations = 0;

        _fixture.Options.Experimental.EnableLogs = true;
        _fixture.Options.Experimental.SetBeforeSendLog((SentryLog log) =>
        {
            invocations++;
            return null;
        });
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
    }

    [Fact]
    public void Log_InvalidFormat_DoesNotCaptureEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}, {4}", ["string", true, 1, 2.2]);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Error);
        entry.Message.Should().Be("Template string does not match the provided argument. The Log will be dropped.");
        entry.Exception.Should().BeOfType<FormatException>();
        entry.Args.Should().BeEmpty();
    }

    [Fact]
    public void Log_InvalidConfigureLog_DoesNotCaptureEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], static (SentryLog log) => throw new InvalidOperationException());

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Error);
        entry.Message.Should().Be("The configureLog callback threw an exception. The Log will be dropped.");
        entry.Exception.Should().BeOfType<InvalidOperationException>();
        entry.Args.Should().BeEmpty();
    }

    [Fact]
    public void Log_InvalidBeforeSendLog_DoesNotCaptureEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        _fixture.Options.Experimental.SetBeforeSendLog(static (SentryLog log) => throw new InvalidOperationException());
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2]);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Error);
        entry.Message.Should().Be("The BeforeSendLog callback threw an exception. The Log will be dropped.");
        entry.Exception.Should().BeOfType<InvalidOperationException>();
        entry.Args.Should().BeEmpty();
    }

    [Fact]
    public void Flush_AfterLog_CapturesEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Flush();
        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.Should().BeNull();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);
        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.Should().BeNull();

        logger.Flush();
        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.AssertEnvelope(_fixture, SentryLogLevel.Trace);
    }

    [Fact]
    public void Dispose_BeforeLog_DoesNotCaptureEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        var defaultLogger = logger.Should().BeOfType<DefaultSentryStructuredLogger>().Which;
        defaultLogger.Dispose();
        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Info);
        entry.Message.Should().Be("Log Buffer full ... dropping log");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEmpty();
    }

    private static void ConfigureLog(SentryLog log)
    {
        log.SetAttribute("attribute-key", "attribute-value");
    }
}

file static class AssertionExtensions
{
    public static void AssertEnvelope(this Envelope envelope, SentryStructuredLoggerTests.Fixture fixture, SentryLogLevel level)
    {
        envelope.Header.Should().ContainSingle().Which.Key.Should().Be("sdk");
        var item = envelope.Items.Should().ContainSingle().Which;

        var log = item.Payload.Should().BeOfType<JsonSerializable>().Which.Source.Should().BeOfType<StructuredLog>().Which;
        AssertLog(log, fixture, level);

        Assert.Collection(item.Header,
            element => Assert.Equal(CreateHeader("type", "log"), element),
            element => Assert.Equal(CreateHeader("item_count", 1), element),
            element => Assert.Equal(CreateHeader("content_type", "application/vnd.sentry.items.log+json"), element));
    }

    public static void AssertLog(this StructuredLog log, SentryStructuredLoggerTests.Fixture fixture, SentryLogLevel level)
    {
        var items = log.Items;
        items.Length.Should().Be(1);
        AssertLog(items[0], fixture, level);
    }

    public static void AssertLog(this SentryLog log, SentryStructuredLoggerTests.Fixture fixture, SentryLogLevel level)
    {
        log.Timestamp.Should().Be(fixture.Clock.GetUtcNow());
        log.TraceId.Should().Be(fixture.TraceId);
        log.Level.Should().Be(level);
        log.Message.Should().Be("Template string with arguments: string, True, 1, 2.2");
        log.Template.Should().Be("Template string with arguments: {0}, {1}, {2}, {3}");
        log.Parameters.Should().BeEquivalentTo(new KeyValuePair<string, object>[] { new("0", "string"), new("1", true), new("2", 1), new("3", 2.2), });
        log.ParentSpanId.Should().Be(fixture.ParentSpanId);
        log.TryGetAttribute("attribute-key", out string? value).Should().BeTrue();
        value.Should().Be("attribute-value");
    }

    private static KeyValuePair<string, object?> CreateHeader(string name, object? value)
    {
        return new KeyValuePair<string, object?>(name, value);
    }
}

file static class SentryStructuredLoggerExtensions
{
    public static void Log(this SentryStructuredLogger logger, SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        switch (level)
        {
            case SentryLogLevel.Trace:
                logger.LogTrace(template, parameters, configureLog);
                break;
            case SentryLogLevel.Debug:
                logger.LogDebug(template, parameters, configureLog);
                break;
            case SentryLogLevel.Info:
                logger.LogInfo(template, parameters, configureLog);
                break;
            case SentryLogLevel.Warning:
                logger.LogWarning(template, parameters, configureLog);
                break;
            case SentryLogLevel.Error:
                logger.LogError(template, parameters, configureLog);
                break;
            case SentryLogLevel.Fatal:
                logger.LogFatal(template, parameters, configureLog);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
