#nullable enable

namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
/// </summary>
public partial class SentryStructuredLoggerTests : IDisposable
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

            var span = Substitute.For<ISpan>();
            span.TraceId.Returns(TraceId);
            span.SpanId.Returns(ParentSpanId.Value);
            Hub.GetSpan().Returns(span);

            ExpectedAttributes = new Dictionary<string, string>(1)
            {
                { "attribute-key", "attribute-value" },
            };
        }

        public InMemoryDiagnosticLogger DiagnosticLogger { get; }
        public IHub Hub { get; }
        public SentryOptions Options { get; }
        public ISystemClock Clock { get; }
        public int BatchSize { get; set; }
        public TimeSpan BatchTimeout { get; set; }
        public SentryId TraceId { get; private set; }
        public SpanId? ParentSpanId { get; private set; }

        public Dictionary<string, string> ExpectedAttributes { get; }

        public void WithoutActiveSpan()
        {
            Hub.GetSpan().Returns((ISpan?)null);

            var scope = new Scope();
            Hub.SubstituteConfigureScope(scope);
            TraceId = scope.PropagationContext.TraceId;
            ParentSpanId = null;
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

    [Fact]
    public void Log_WithoutActiveSpan_CapturesEnvelope()
    {
        _fixture.WithoutActiveSpan();
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.LogTrace(ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelope(envelope, SentryLogLevel.Trace);
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

        logger.LogTrace(ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
        _fixture.AssertLog(configuredLog, SentryLogLevel.Trace);
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

        logger.LogTrace(ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
    }

    [Fact]
    public void Log_InvalidFormat_DoesNotCaptureEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}, {4}", "string", true, 1, 2.2);

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

        logger.LogTrace(static (SentryLog log) => throw new InvalidOperationException(), "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);

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

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);

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

        logger.LogTrace(ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.Should().BeNull();

        logger.Flush();
        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelope(envelope, SentryLogLevel.Trace);
    }

    [Fact]
    public void Dispose_BeforeLog_DoesNotCaptureEnvelope()
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        var defaultLogger = logger.Should().BeOfType<DefaultSentryStructuredLogger>().Which;
        defaultLogger.Dispose();
        logger.LogTrace(ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);

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

internal static class AssertionExtensions
{
    public static void AssertEnvelope(this SentryStructuredLoggerTests.Fixture fixture, Envelope envelope, SentryLogLevel level)
    {
        envelope.Header.Should().ContainSingle().Which.Key.Should().Be("sdk");
        var item = envelope.Items.Should().ContainSingle().Which;

        var log = item.Payload.Should().BeOfType<JsonSerializable>().Which.Source.Should().BeOfType<StructuredLog>().Which;
        AssertLog(fixture, log, level);

        Assert.Collection(item.Header,
            element => Assert.Equal(CreateHeader("type", "log"), element),
            element => Assert.Equal(CreateHeader("item_count", 1), element),
            element => Assert.Equal(CreateHeader("content_type", "application/vnd.sentry.items.log+json"), element));
    }

    public static void AssertEnvelopeWithoutAttributes(this SentryStructuredLoggerTests.Fixture fixture, Envelope envelope, SentryLogLevel level)
    {
        fixture.ExpectedAttributes.Clear();
        AssertEnvelope(fixture, envelope, level);
    }

    public static void AssertLog(this SentryStructuredLoggerTests.Fixture fixture, StructuredLog log, SentryLogLevel level)
    {
        var items = log.Items;
        items.Length.Should().Be(1);
        AssertLog(fixture, items[0], level);
    }

    public static void AssertLog(this SentryStructuredLoggerTests.Fixture fixture, SentryLog log, SentryLogLevel level)
    {
        log.Timestamp.Should().Be(fixture.Clock.GetUtcNow());
        log.TraceId.Should().Be(fixture.TraceId);
        log.Level.Should().Be(level);
        log.Message.Should().Be("Template string with arguments: string, True, 1, 2.2");
        log.Template.Should().Be("Template string with arguments: {0}, {1}, {2}, {3}");
        log.Parameters.Should().BeEquivalentTo(new KeyValuePair<string, object>[] { new("0", "string"), new("1", true), new("2", 1), new("3", 2.2), });
        log.ParentSpanId.Should().Be(fixture.ParentSpanId);

        foreach (var expectedAttribute in fixture.ExpectedAttributes)
        {
            log.TryGetAttribute(expectedAttribute.Key, out string? value).Should().BeTrue();
            value.Should().Be(expectedAttribute.Value);
        }
    }

    private static KeyValuePair<string, object?> CreateHeader(string name, object? value)
    {
        return new KeyValuePair<string, object?>(name, value);
    }

    public static SentryLog ShouldContainSingleLog(this Envelope envelope)
    {
        var envelopeItem = envelope.Items.Should().ContainSingle().Which;
        var serializable = envelopeItem.Payload.Should().BeOfType<JsonSerializable>().Which;
        var log = serializable.Source.Should().BeOfType<StructuredLog>().Which;

        log.Items.Length.Should().Be(1);
        return log.Items[0];
    }
}
