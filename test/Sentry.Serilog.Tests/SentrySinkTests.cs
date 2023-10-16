namespace Sentry.Serilog.Tests;

public class SentrySinkTests
{
    private class Fixture
    {
        public SentrySerilogOptions Options { get; set; } = new();
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Func<IHub> HubAccessor { get; set; }
        public IDisposable SdkDisposeHandle { get; set; } = Substitute.For<IDisposable>();
        public Scope Scope { get; } = new(new SentryOptions());

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
            HubAccessor = () => Hub;
            Hub.ConfigureScope(Arg.Invoke(Scope));
        }

        public SentrySink GetSut()
            => new(
                Options,
                HubAccessor,
                SdkDisposeHandle,
                new MockClock());
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Emit_WithException_CreatesEventWithException()
    {
        var sut = _fixture.GetSut();

        var expected = new Exception("expected");

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, expected, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        sut.Emit(evt);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception == expected));
    }

    [Fact]
    public void Emit_WithException_BreadcrumbFromException()
    {
        var expectedException = new Exception("expected message");
        const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Fatal, expectedException, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        sut.Emit(evt);

        var b = _fixture.Scope.Breadcrumbs.First();
        Assert.Equal(expectedException.Message, b.Message);
        Assert.Equal(DateTimeOffset.MaxValue, b.Timestamp);
        Assert.Null(b.Category);
        Assert.Equal(expectedLevel, b.Level);
        Assert.Null(b.Type);
        Assert.Null(b.Data);
    }

    [Fact]
    public void Emit_SerilogSdk_Name()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        sut.Emit(evt);

        var expected = typeof(SentrySink).Assembly.GetNameAndVersion();
        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Sdk.Name == SentrySink.SdkName
                                                   && e.Sdk.Version == expected.Version));
    }

    [Fact]
    public void Emit_SerilogSdk_Packages()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        SentryEvent actual = null;
        _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
            .Do(c => actual = c.Arg<SentryEvent>());

        sut.Emit(evt);

        var expected = typeof(SentrySink).Assembly.GetNameAndVersion();

        Assert.NotNull(actual);
        var package = Assert.Single(actual.Sdk.Packages);
        Assert.Equal("nuget:" + expected.Name, package!.Name);
        Assert.Equal(expected.Version, package.Version);
    }

    internal class EventLogLevelsData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { LogEventLevel.Debug, SentryLevel.Debug };
            yield return new object[] { LogEventLevel.Verbose, SentryLevel.Debug };
            yield return new object[] { LogEventLevel.Information, SentryLevel.Info };
            yield return new object[] { LogEventLevel.Warning, SentryLevel.Warning };
            yield return new object[] { LogEventLevel.Error, SentryLevel.Error };
            yield return new object[] { LogEventLevel.Fatal, SentryLevel.Fatal };
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(EventLogLevelsData))]
    public void Emit_LoggerLevel_Set(LogEventLevel serilogLevel, SentryLevel? sentryLevel)
    {
        // Make sure test cases are not filtered out by the default min levels:
        _fixture.Options.MinimumEventLevel = LogEventLevel.Verbose;
        _fixture.Options.MinimumBreadcrumbLevel = LogEventLevel.Verbose;

        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, serilogLevel, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());

        sut.Emit(evt);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == sentryLevel));
    }

    [Fact]
    public void Emit_RenderedMessage_Set()
    {
        const string expected = "message";

        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse(expected), Enumerable.Empty<LogEventProperty>());

        sut.Emit(evt);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Formatted == expected));
    }

    [Fact]
    public void Emit_HubAccessorReturnsNull_DoesNotThrow()
    {
        _fixture.HubAccessor = () => null;
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
        sut.Emit(evt);
    }

    [Fact]
    public void Emit_DisabledHub_CaptureNotCalled()
    {
        _fixture.Hub.IsEnabled.Returns(false);
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
        sut.Emit(evt);

        _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Emit_EnabledHub_CaptureCalled()
    {
        _fixture.Hub.IsEnabled.Returns(true);
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
        sut.Emit(evt);

        _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Emit_Properties_AsExtra()
    {
        const string expectedIp = "127.0.0.1";

        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            new[] { new LogEventProperty("IPAddress", new ScalarValue(expectedIp)) });

        sut.Emit(evt);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Data["IPAddress"].ToString() == expectedIp));
    }

    [Fact]
    public void Close_DisposesSdk()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
        sut.Emit(evt);

        _fixture.SdkDisposeHandle.DidNotReceive().Dispose();

        sut.Dispose();

        _fixture.SdkDisposeHandle.Received(1).Dispose();
    }

    [Fact]
    public void Close_NoDisposeHandleProvided_DoesNotThrow()
    {
        _fixture.SdkDisposeHandle = null;
        var sut = _fixture.GetSut();
        sut.Dispose();
    }

    [Fact]
    public void Emit_WithFormat_EventCaptured()
    {
        const string expectedMessage = "Test {structured} log";
        const int param = 10;

        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse(expectedMessage),
            new[] { new LogEventProperty("structured", new ScalarValue(param)) });

        sut.Emit(evt);

        _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
            p.Message.Formatted == $"Test {param} log"
            && p.Message.Message == expectedMessage));
    }

    [Fact]
    public void Emit_WithTextFormatter_EventCaptured()
    {
        const string expectedMessage = "Test log with formatter";
        const int param = 10;

        // Use custom TextFormatter
        _fixture.Options.TextFormatter = new MessageTemplateTextFormatter("[{structured}] {Message}");

        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse(expectedMessage),
            new[] { new LogEventProperty("structured", new ScalarValue(param)) });

        sut.Emit(evt);

        _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
            p.Message.Formatted == $"[{param}] Test log with formatter"
            && p.Message.Message == expectedMessage));
    }

    [Fact]
    public void Emit_SourceContextMatchesSentry_NoEventSent()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse("message"),
            new[] { new LogEventProperty("SourceContext", new ScalarValue("Sentry.Serilog")) });

        sut.Emit(evt);

        _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Emit_SourceContextContainsSentry_NoEventSent()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse("message"),
            new[] { new LogEventProperty("SourceContext", new ScalarValue("Sentry")) });

        sut.Emit(evt);

        _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Emit_SourceContextMatchesSentry_NoScopeConfigured()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse("message"),
            new[] { new LogEventProperty("SourceContext", new ScalarValue("Sentry.Serilog")) });

        sut.Emit(evt);

        _fixture.Hub.DidNotReceive().ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Fact]
    public void Emit_SourceContextContainsSentry_NoScopeConfigured()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse("message"),
            new[] { new LogEventProperty("SourceContext", new ScalarValue("Sentry")) });

        sut.Emit(evt);

        _fixture.Hub.DidNotReceive().ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Fact]
    public void Emit_WithSourceContext_LoggerNameEquals()
    {
        var sut = _fixture.GetSut();

        const string expectedLogger = "LoggerName";
        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse("message"),
            new[] { new LogEventProperty("SourceContext", new ScalarValue(expectedLogger)) });

        sut.Emit(evt);

        _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
            p.Logger == expectedLogger));
    }

    [Fact]
    public void Emit_NoSourceContext_LoggerNameNull()
    {
        var sut = _fixture.GetSut();

        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
            new MessageTemplateParser().Parse("message"),
            new LogEventProperty[0]);

        sut.Emit(evt);

        _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
            p.Logger == null));
    }
}
