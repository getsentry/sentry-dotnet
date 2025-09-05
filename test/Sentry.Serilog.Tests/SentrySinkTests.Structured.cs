#nullable enable

namespace Sentry.Serilog.Tests;

public partial class SentrySinkTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Emit_StructuredLogging_IsEnabled(bool isEnabled)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.Experimental.EnableLogs = isEnabled;

        var sut = _fixture.GetSut();
        var logger = new LoggerConfiguration().WriteTo.Sink(sut).MinimumLevel.Verbose().CreateLogger();

        logger.Write(LogEventLevel.Information, "Message");

        capturer.Logs.Should().HaveCount(isEnabled ? 1 : 0);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, SentryLogLevel.Trace)]
    [InlineData(LogEventLevel.Debug, SentryLogLevel.Debug)]
    [InlineData(LogEventLevel.Information, SentryLogLevel.Info)]
    [InlineData(LogEventLevel.Warning, SentryLogLevel.Warning)]
    [InlineData(LogEventLevel.Error, SentryLogLevel.Error)]
    [InlineData(LogEventLevel.Fatal, SentryLogLevel.Fatal)]
    public void Emit_StructuredLogging_LogLevel(LogEventLevel level, SentryLogLevel expected)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.Experimental.EnableLogs = true;

        var sut = _fixture.GetSut();
        var logger = new LoggerConfiguration().WriteTo.Sink(sut).MinimumLevel.Verbose().CreateLogger();

        logger.Write(level, "Message");

        capturer.Logs.Should().ContainSingle().Which.Level.Should().Be(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Emit_StructuredLogging_LogEvent(bool withTraceHeader)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.Experimental.EnableLogs = true;
        _fixture.Options.Environment = "test-environment";
        _fixture.Options.Release = "test-release";

        var traceHeader = new SentryTraceHeader(SentryId.Create(), SpanId.Create(), null);
        _fixture.Hub.GetTraceHeader().Returns(withTraceHeader ? traceHeader : null);

        var sut = _fixture.GetSut();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(sut)
            .MinimumLevel.Verbose()
            .Enrich.WithProperty("Scalar-Property", 42)
            .Enrich.WithProperty("Sequence-Property", new[] { 41, 42, 43 })
            .Enrich.WithProperty("Dictionary-Property", new Dictionary<string, string> { { "key", "value" } })
            .Enrich.WithProperty("Structure-Property", (Number: 42, Text: "42"))
            .CreateLogger();

        logger.Write(LogEventLevel.Information,
            "Message with Scalar property {Scalar}, Sequence property: {Sequence}, Dictionary property: {Dictionary}, and Structure property: {Structure}.",
            42, new[] { 41, 42, 43 }, new Dictionary<string, string> { { "key", "value" } }, (Number: 42, Text: "42"));

        var log = capturer.Logs.Should().ContainSingle().Which;
        log.Timestamp.Should().BeOnOrBefore(DateTimeOffset.Now);
        log.TraceId.Should().Be(withTraceHeader ? traceHeader.TraceId : SentryId.Empty);
        log.Level.Should().Be(SentryLogLevel.Info);
        log.Message.Should().Be("""Message with Scalar property 42, Sequence property: [41, 42, 43], Dictionary property: [("key": "value")], and Structure property: [42, "42"].""");
        log.Template.Should().Be("Message with Scalar property {Scalar}, Sequence property: {Sequence}, Dictionary property: {Dictionary}, and Structure property: {Structure}.");
        log.Parameters.Should().HaveCount(4);
        log.Parameters[0].Should().BeEquivalentTo(new KeyValuePair<string, int>("Scalar", 42));
        log.Parameters[1].Should().BeEquivalentTo(new KeyValuePair<string, string>("Sequence", "[41, 42, 43]"));
        log.Parameters[2].Should().BeEquivalentTo(new KeyValuePair<string, string>("Dictionary", """[("key": "value")]"""));
        log.Parameters[3].Should().BeEquivalentTo(new KeyValuePair<string, string>("Structure", """[42, "42"]"""));
        log.ParentSpanId.Should().Be(withTraceHeader ? traceHeader.SpanId : SpanId.Empty);

        log.TryGetAttribute("sentry.environment", out object? environment).Should().BeTrue();
        environment.Should().Be("test-environment");
        log.TryGetAttribute("sentry.release", out object? release).Should().BeTrue();
        release.Should().Be("test-release");
        log.TryGetAttribute("sentry.sdk.name", out object? sdkName).Should().BeTrue();
        sdkName.Should().Be(SentrySink.SdkName);
        log.TryGetAttribute("sentry.sdk.version", out object? sdkVersion).Should().BeTrue();
        sdkVersion.Should().Be(SentrySink.NameAndVersion.Version);

        log.TryGetAttribute("property.Scalar-Property", out object? scalar).Should().BeTrue();
        scalar.Should().Be(42);
        log.TryGetAttribute("property.Sequence-Property", out object? sequence).Should().BeTrue();
        sequence.Should().Be("[41, 42, 43]");
        log.TryGetAttribute("property.Dictionary-Property", out object? dictionary).Should().BeTrue();
        dictionary.Should().Be("""[("key": "value")]""");
        log.TryGetAttribute("property.Structure-Property", out object? structure).Should().BeTrue();
        structure.Should().Be("""[42, "42"]""");
    }
}
