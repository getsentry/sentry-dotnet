#nullable enable

using log4net.Util;

namespace Sentry.Log4Net.Tests;

public partial class SentryAppenderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoAppend_StructuredLogging_IsEnabled(bool isEnabled)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = isEnabled;

        var sut = _fixture.GetSut();

        sut.DoAppend(CreateLoggingEvent(Level.Info, "Message"));

        capturer.Logs.Should().HaveCount(isEnabled ? 1 : 0);
    }

    public static TheoryData<Level, SentryLogLevel> LogLevelData => new()
    {
        { Level.All, SentryLogLevel.Trace },
        { Level.Finest, SentryLogLevel.Trace },
        { Level.Verbose, SentryLogLevel.Trace },
        { Level.Finer, SentryLogLevel.Trace },
        { Level.Trace, SentryLogLevel.Trace },
        { Level.Fine, SentryLogLevel.Debug },
        { Level.Debug, SentryLogLevel.Debug },
        { Level.Info, SentryLogLevel.Info },
        { Level.Notice, SentryLogLevel.Info },
        { Level.Warn, SentryLogLevel.Warning },
        { Level.Error, SentryLogLevel.Error },
        { Level.Severe, SentryLogLevel.Error },
        { Level.Critical, SentryLogLevel.Error },
        { Level.Alert, SentryLogLevel.Error },
        { Level.Fatal, SentryLogLevel.Fatal },
        { Level.Emergency, SentryLogLevel.Fatal },
        { Level.Log4Net_Debug, SentryLogLevel.Fatal },
        { new Level(0, "DEFAULT"), SentryLogLevel.Trace },
        { new Level(-1, "CUSTOM"), SentryLogLevel.Trace },
    };

    [Theory]
    [MemberData(nameof(LogLevelData))]
    public void DoAppend_StructuredLogging_LogLevel(Level level, SentryLogLevel expected)
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var sut = _fixture.GetSut();

        sut.DoAppend(CreateLoggingEvent(level, "Message"));

        capturer.Logs.Should().ContainSingle().Which.Level.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoAppend_StructuredLogging_LogEvent(bool withActiveSpan)
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

        var sut = _fixture.GetSut();
        ThreadContext.Properties["Text-Property"] = "4";
        ThreadContext.Properties["Number-Property"] = 4;
        ThreadContext.Properties["Collection-Property"] = new[] { 3, 4, 5 };
        ThreadContext.Properties["Object-Property"] = (Number: 4, Text: "4");

        sut.DoAppend(CreateLoggingEvent(Level.Info, "{0}, {1}, {2}", [0, 1, 2]));

        var log = capturer.Logs.Should().ContainSingle().Which;
        log.Timestamp.Should().BeOnOrBefore(DateTimeOffset.Now);
        log.TraceId.Should().Be(withActiveSpan ? _fixture.Hub.GetSpan()!.TraceId : _fixture.Scope.PropagationContext.TraceId);
        log.Level.Should().Be(SentryLogLevel.Info);
        log.Message.Should().Be("0, 1, 2");
        log.Template.Should().BeNull();
        log.Parameters.Should().BeEmpty();
        log.SpanId.Should().Be(withActiveSpan ? _fixture.Hub.GetSpan()!.SpanId : null);

        log.TryGetAttribute("sentry.environment", out object? environment).Should().BeTrue();
        environment.Should().Be("test-environment");
        log.TryGetAttribute("sentry.release", out object? release).Should().BeTrue();
        release.Should().Be("test-release");
        log.TryGetAttribute("sentry.origin", out object? origin).Should().BeTrue();
        origin.Should().Be("auto.log.log4net");
        log.TryGetAttribute("sentry.sdk.name", out object? sdkName).Should().BeTrue();
        sdkName.Should().Be(SentryAppender.SdkName);
        log.TryGetAttribute("sentry.sdk.version", out object? sdkVersion).Should().BeTrue();
        sdkVersion.Should().Be(SentryAppender.NameAndVersion.Version);

        log.TryGetAttribute("property.Text-Property", out object? text).Should().BeTrue();
        text.Should().Be("4");
        log.TryGetAttribute("property.Number-Property", out object? number).Should().BeTrue();
        number.Should().Be(4);
        log.TryGetAttribute("property.Collection-Property", out object? collection).Should().BeTrue();
        collection.Should().BeEquivalentTo(new[] { 3, 4, 5 });
        log.TryGetAttribute("property.Object-Property", out object? obj).Should().BeTrue();
        obj.Should().Be((Number: 4, Text: "4"));
    }

    [Fact]
    public void DoAppend_StructuredLogging_Properties()
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var sut = _fixture.GetSut();

        LoggingEventData data = new()
        {
            LoggerName = "TestLogger",
            Level = Level.Info,
            Message = "Test Message",
            ThreadName = "1",
            LocationInfo = new LocationInfo(null),
            UserName = "TestUser",
            Identity = "TestIdentity",
            ExceptionString = "Exception",
            Domain = "TestDomain",
            Properties = new PropertiesDictionary(),
            TimeStampUtc = DateTime.UtcNow,
        };
        LoggingEvent loggingEvent = new(data);
        sut.DoAppend(loggingEvent);

        var log = capturer.Logs.Should().ContainSingle().Which;
        log.Level.Should().Be(SentryLogLevel.Info);
        log.Message.Should().Be("Test Message");

        //TODO: assert Count/Length of Attributes
        //requires: https://github.com/getsentry/sentry-dotnet/pull/4936
        //should not contain "log4net:.." properties
    }

    [Fact]
    public void DoAppend_StructuredLoggingWithException_NoBreadcrumb()
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var sut = _fixture.GetSut();
        sut.MinimumEventLevel = Level.Error;

        sut.DoAppend(CreateLoggingEvent(Level.Error, "Message", new Exception("expected message")));

        capturer.Logs.Should().ContainSingle().Which.Message.Should().Be("Message");
        _fixture.Scope.Breadcrumbs.Should().BeEmpty();
        _ = _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void DoAppend_StructuredLoggingWithoutException_LeavesBreadcrumb()
    {
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        _fixture.Options.EnableLogs = true;

        var sut = _fixture.GetSut();
        sut.MinimumEventLevel = Level.Fatal;

        sut.DoAppend(CreateLoggingEvent(Level.Error, "Message"));

        capturer.Logs.Should().ContainSingle().Which.Message.Should().Be("Message");
        _fixture.Scope.Breadcrumbs.Should().ContainSingle().Which.Message.Should().Be("Message");
        _ = _fixture.Hub.Received(0).CaptureEvent(Arg.Any<SentryEvent>());
    }

    private static LoggingEvent CreateLoggingEvent(Level level, string message, Exception? exception = null)
    {
        return new LoggingEvent(null, null, "TestLogger", level, message, exception);
    }

    private static LoggingEvent CreateLoggingEvent(Level level, string format, object[] args)
    {
        var message = new SystemStringFormat(CultureInfo.InvariantCulture, format, args);
        return new LoggingEvent(null, null, "TestLogger", level, message, null);
    }
}
