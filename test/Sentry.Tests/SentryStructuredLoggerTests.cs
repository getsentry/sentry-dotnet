#nullable enable

namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
/// </summary>
public class SentryStructuredLoggerTests
{
    internal sealed class Fixture
    {
        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            ScopeManager = Substitute.For<IInternalScopeManager>();
            Options = new SentryOptions();
            Clock = new MockClock(new DateTimeOffset(2025, 04, 22, 14, 51, 00, TimeSpan.Zero));
            Span = Substitute.For<ISpan>();
            TraceId = SentryId.Create();
            ParentSpanId = SpanId.Create();

            Hub.GetSpan().Returns(Span);
            Span.TraceId.Returns(TraceId);
            Span.ParentSpanId.Returns(ParentSpanId);
        }

        public IHub Hub { get; }
        public IInternalScopeManager ScopeManager { get; }
        public SentryOptions Options { get; }
        public ISystemClock Clock { get; }
        public ISpan Span { get; }
        public SentryId TraceId { get; }
        public SpanId? ParentSpanId { get; }

        public void UseScopeManager()
        {
            Hub.GetSpan().Returns((ISpan?)null);

            var propagationContext = new SentryPropagationContext(TraceId, ParentSpanId!.Value);
            var scope = new Scope(Options, propagationContext);
            var scopeAndClient = new KeyValuePair<Scope, ISentryClient>(scope, null!);
            ScopeManager.GetCurrent().Returns(scopeAndClient);
        }

        public SentryStructuredLogger GetSut() => new(Hub, ScopeManager, Options, Clock);
    }

    private readonly Fixture _fixture;

    public SentryStructuredLoggerTests()
    {
        _fixture = new Fixture();
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
        _fixture.Options.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

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
        _fixture.Options.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetSut();

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Fact]
    public void Log_UseScopeManager_CapturesEnvelope()
    {
        _fixture.UseScopeManager();
        _fixture.Options.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.AssertEnvelope(_fixture, SentryLogLevel.Trace);
    }

    [Fact]
    public void Log_WithBeforeSendLog_InvokesCallback()
    {
        var invocations = 0;
        SentryLog configuredLog = null!;

        _fixture.Options.EnableLogs = true;
        _fixture.Options.SetBeforeSendLog((SentryLog log) =>
        {
            invocations++;
            configuredLog = log;
            return log;
        });
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
        configuredLog.AssertLog(_fixture, SentryLogLevel.Trace);
    }

    [Fact]
    public void Log_WhenBeforeSendLogReturnsNull_DoesNotCaptureEnvelope()
    {
        var invocations = 0;

        _fixture.Options.EnableLogs = true;
        _fixture.Options.SetBeforeSendLog((SentryLog log) =>
        {
            invocations++;
            return null;
        });
        var logger = _fixture.GetSut();

        logger.LogTrace("Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
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

        var log = item.Payload.Should().BeOfType<JsonSerializable>().Which.Source.Should().BeOfType<SentryLog>().Which;
        AssertLog(log, fixture, level);

        Assert.Collection(item.Header,
            element => Assert.Equal(CreateHeader("type", "log"), element),
            element => Assert.Equal(CreateHeader("item_count", 1), element),
            element => Assert.Equal(CreateHeader("content_type", "application/vnd.sentry.items.log+json"), element));
    }

    public static void AssertLog(this SentryLog log, SentryStructuredLoggerTests.Fixture fixture, SentryLogLevel level)
    {
        log.Timestamp.Should().Be(fixture.Clock.GetUtcNow());
        log.TraceId.Should().Be(fixture.TraceId);
        log.Level.Should().Be(level);
        log.Message.Should().Be("Template string with arguments: string, True, 1, 2.2");
        log.Template.Should().Be("Template string with arguments: {0}, {1}, {2}, {3}");
        log.Parameters.Should().BeEquivalentTo(new object[] { "string", true, 1, 2.2 } );
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
