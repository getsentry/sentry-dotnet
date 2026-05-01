#nullable enable

namespace Sentry.NLog.Tests;

public partial class SentryTargetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Write_StructuredLogging_IsEnabled(bool isEnabled)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = isEnabled;

        var logger = _fixture.GetLogger();

        logger.Info("Message");

        capturer.Logs.Should().HaveCount(isEnabled ? 1 : 0);
    }

    public static TheoryData<LogLevel, SentryLogLevel> SentryLogLevelData => new()
    {
        { LogLevel.Trace, SentryLogLevel.Trace },
        { LogLevel.Debug, SentryLogLevel.Debug },
        { LogLevel.Info, SentryLogLevel.Info },
        { LogLevel.Warn, SentryLogLevel.Warning },
        { LogLevel.Error, SentryLogLevel.Error },
        { LogLevel.Fatal, SentryLogLevel.Fatal },
    };

    [Theory]
    [MemberData(nameof(SentryLogLevelData))]
    public void Write_StructuredLogging_LogLevel(LogLevel level, SentryLogLevel expected)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var logger = _fixture.GetLogger();

        logger.Log(level, "Message");

        capturer.Logs.Should().ContainSingle().Which.Level.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_StructuredLogging_LogEvent(bool withActiveSpan)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;
        _fixture.Options.Environment = "test-environment";
        _fixture.Options.Release = "test-release";

        if (withActiveSpan)
        {
            var span = Substitute.For<ISpan>();
            span.TraceId.Returns(SentryId.Create());
            span.SpanId.Returns(SpanId.Create());
            _fixture.Hub.GetSpan().Returns(span);
        }
        else
        {
            _fixture.Hub.GetSpan().Returns((ISpan?)null);
        }

        var logger = _fixture.GetLogger()
            .WithProperty("Text-Property-Key", "Text-Property-Value")
            .WithProperty("Number-Property", 42)
            .WithProperty("Collection-Property", new[] { 41, 42, 43 })
            .WithProperty("Map-Property", new Dictionary<string, string> { { "key", "value" } })
            .WithProperty("Object-Property", (Number: 42, Text: "42"));

        logger.Info("{}, {Text}, {Number}, {Collection}, {Map}, {Object}.",
            null, "Text", 42, new[] { 41, 42, 43 }, new Dictionary<string, string> { { "key", "value" } }, (Number: 42, Text: "42"));

        var log = capturer.Logs.Should().ContainSingle().Which;
        log.Timestamp.Should().BeOnOrBefore(DateTimeOffset.Now);
        log.TraceId.Should().Be(withActiveSpan ? _fixture.Hub.GetSpan()!.TraceId : _fixture.Scope.PropagationContext.TraceId);
        log.Level.Should().Be(SentryLogLevel.Info);
        log.Message.Should().Be("""NULL, "Text", 42, 41, 42, 43, "key"="value", (42, 42).""");
        log.Template.Should().Be("{}, {Text}, {Number}, {Collection}, {Map}, {Object}.");
        log.Parameters.Should().HaveCount(6);
        log.Parameters[0].Should().BeEquivalentTo(new KeyValuePair<string, object?>("", null));
        log.Parameters[1].Should().BeEquivalentTo(new KeyValuePair<string, object>("Text", "Text"));
        log.Parameters[2].Should().BeEquivalentTo(new KeyValuePair<string, object>("Number", 42));
        log.Parameters[3].Should().BeEquivalentTo(new KeyValuePair<string, object>("Collection", new[] { 41, 42, 43 }));
        log.Parameters[4].Should().BeEquivalentTo(new KeyValuePair<string, object>("Map", new Dictionary<string, string> { { "key", "value" } }));
        log.Parameters[5].Should().BeEquivalentTo(new KeyValuePair<string, object>("Object", (Number: 42, Text: "42")));
        log.SpanId.Should().Be(withActiveSpan ? _fixture.Hub.GetSpan()!.SpanId : null);

        log.TryGetAttribute("sentry.environment", out object? environment).Should().BeTrue();
        environment.Should().Be("test-environment");
        log.TryGetAttribute("sentry.release", out object? release).Should().BeTrue();
        release.Should().Be("test-release");
        log.TryGetAttribute("sentry.origin", out object? origin).Should().BeTrue();
        origin.Should().Be("auto.log.nlog");
        log.TryGetAttribute("sentry.sdk.name", out object? sdkName).Should().BeTrue();
        sdkName.Should().Be(Constants.SdkName);
        log.TryGetAttribute("sentry.sdk.version", out object? sdkVersion).Should().BeTrue();
        sdkVersion.Should().Be(SentryTarget.NameAndVersion.Version);

        log.TryGetAttribute("property.Text-Property-Key", out object? text).Should().BeTrue();
        text.Should().Be("Text-Property-Value");
        log.TryGetAttribute("property.Number-Property", out object? number).Should().BeTrue();
        number.Should().Be(42);
        log.TryGetAttribute("property.Collection-Property", out object? collection).Should().BeTrue();
        collection.Should().BeEquivalentTo(new[] { 41, 42, 43 });
        log.TryGetAttribute("property.Map-Property", out object? map).Should().BeTrue();
        map.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
        log.TryGetAttribute("property.Object-Property", out object? obj).Should().BeTrue();
        obj.Should().Be((Number: 42, Text: "42"));

        log.TryGetAttribute("property.Text", out object? _).Should().BeFalse();
        log.TryGetAttribute("property.Number", out object? _).Should().BeFalse();
        log.TryGetAttribute("property.Collection", out object? _).Should().BeFalse();
        log.TryGetAttribute("property.Map", out object? _).Should().BeFalse();
        log.TryGetAttribute("property.Object", out object? _).Should().BeFalse();
    }

    [Fact]
    public void Write_StructuredLogging_IsPositional()
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var logger = _fixture.GetLogger();

        logger.Info("{0}, {1}, {2}, {3}.", 0, 1, 2, 3);

        capturer.Logs.Should().ContainSingle().Which.Parameters.Should().BeEquivalentTo([
            new KeyValuePair<string, object>("0", 0),
            new KeyValuePair<string, object>("1", 1),
            new KeyValuePair<string, object>("2", 2),
            new KeyValuePair<string, object>("3", 3),
        ]);
    }

    [Fact]
    public void Write_StructuredLoggingWithException_NoBreadcrumb()
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var logger = _fixture.GetLogger();

        logger.Error(new Exception("expected message"), "Message");

        capturer.Logs.Should().ContainSingle().Which.Message.Should().Be("Message");
        _fixture.Scope.Breadcrumbs.Should().BeEmpty();
        _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Write_StructuredLoggingWithoutException_LeavesBreadcrumb()
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var logger = _fixture.GetLogger();

        logger.Error((Exception?)null, "Message");

        capturer.Logs.Should().ContainSingle().Which.Message.Should().Be("Message");
        _fixture.Scope.Breadcrumbs.Should().ContainSingle().Which.Message.Should().Be("Message");
        _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }
}
