using log4net.Util;

namespace Sentry.Log4Net.Tests;

public class SentryAppenderTests : IDisposable
{
    private class Fixture
    {
        public bool InitInvoked { get; set; }
        public string DsnReceivedOnInit { get; set; }
        public IDisposable SdkDisposeHandle { get; set; } = Substitute.For<IDisposable>();
        public Func<string, IDisposable> InitAction { get; set; }
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Func<IHub> HubAccessor { get; set; }
        public Scope Scope { get; } = new(new SentryOptions());
        public string Dsn { get; set; } = "dsn";
        public SentryOptions Options { get; } = new();

        public Fixture()
        {
            HubAccessor = () => Hub;
            Hub.SubstituteConfigureScope(Scope);
            InitAction = s =>
            {
                DsnReceivedOnInit = s;
                InitInvoked = true;
                return SdkDisposeHandle;
            };
        }

        public SentryAppender GetSut()
        {
            SentryClientExtensions.SentryOptionsForTestingOnly = Options;

            var sut = new SentryAppender(InitAction, Hub)
            {
                Dsn = Dsn
            };
            sut.ActivateOptions();
            return sut;
        }
    }

    private readonly Fixture _fixture = new();

    public void Dispose()
    {
        SentryClientExtensions.SentryOptionsForTestingOnly = null;
    }

    [Fact]
    public void Append_WithException_CreatesEventWithException()
    {
        var sut = _fixture.GetSut();

        var expected = new Exception("expected");

        var evt = new LoggingEvent(null, null, "logger", Level.Critical, null, expected);

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception == expected));
    }

    [Fact]
    public void Append_Log4NetSdk_Name()
    {
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());

        sut.DoAppend(evt);

        var expected = typeof(SentryAppender).Assembly.GetNameAndVersion();
        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Sdk.Name == SentryAppender.SdkName
                                                   && e.Sdk.Version == expected.Version));
    }

    [Fact]
    public void Append_Log4NetSdk_Packages()
    {
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());

        SentryEvent actual = null;
        _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
            .Do(c => actual = c.Arg<SentryEvent>());

        sut.DoAppend(evt);

        var expected = typeof(SentryAppender).Assembly.GetNameAndVersion();

        Assert.NotNull(actual);
        var package = Assert.Single(actual.Sdk.Packages);
        Assert.Equal("nuget:" + expected.Name, package.Name);
        Assert.Equal(expected.Version, package.Version);
    }

    [Fact]
    public void Append_LoggerNameAndLevel_Set()
    {
        const string expectedLogger = "logger";
        const SentryLevel expectedLevel = SentryLevel.Error;

        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(null, null, expectedLogger, Level.Error, null, null);

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Logger == expectedLogger
                                                   && e.Level == expectedLevel));
    }

    [Fact]
    public void Append_RenderedMessage_Set()
    {
        const string expected = "message";

        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(null, null, null, null, expected, null);

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Message == expected));
    }

    [Fact]
    public void Append_NullEvent_NoOp()
    {
        var sut = _fixture.GetSut();

        sut.DoAppend(null as LoggingEvent);
    }

    [Fact]
    public void Append_BelowThreshold_DoesNotSendEvent()
    {
        var sut = _fixture.GetSut();
        sut.Threshold = Level.Warn;
        var evt = new LoggingEvent(new LoggingEventData
        {
            Level = Level.Info
        });

        sut.DoAppend(evt);
        _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Append_ByDefault_DoesNotSetUser()
    {
        var sut = _fixture.GetSut();
        var evt = new LoggingEvent(new LoggingEventData
        {
            Identity = "sentry-user"
        });

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => !e.HasUser()));
    }

    [Fact]
    public void Append_ConfiguredSendUser_UserInEvent()
    {
        const string expected = "sentry-user";
        var sut = _fixture.GetSut();
        var evt = new LoggingEvent(new LoggingEventData
        {
            Identity = expected
        });

        sut.SendIdentity = true;
        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.User.Id == expected));
    }

    [Fact]
    public void Append_NoDsn_InitNotCalled()
    {
        _fixture.Dsn = null;
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        Assert.False(_fixture.InitInvoked);
    }

    [Fact]
    public void Append_WithEnabledHub_InitNotCalled()
    {
        _ = _fixture.Hub.IsEnabled.Returns(true);
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        Assert.False(_fixture.InitInvoked);
    }

    [Fact]
    public void Append_WithDsn_InitCalled()
    {
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        Assert.True(_fixture.InitInvoked);
        Assert.Same(_fixture.Dsn, _fixture.DsnReceivedOnInit);
    }

    [Fact]
    public void Append_NoDsn_HubNotCalled()
    {
        _fixture.Dsn = null;
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        Assert.False(_fixture.InitInvoked);
        _ = _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(null);
    }

    [Fact]
    public void Append_NoDsnAndDisabledHub_HubNotCalled()
    {
        _fixture.Dsn = null;
        _ = _fixture.Hub.IsEnabled.Returns(false);
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        Assert.False(_fixture.InitInvoked);
        _ = _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(null);
    }

    [Fact]
    public void Append_LocationInformation_AsExtra()
    {
        const string expectedClass = "class";
        const string expectedMethod = "method";
        const string expectedFileName = "fileName";
        const int expectedLineNumber = 100;

        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData
        {
            LocationInfo = new LocationInfo(
                expectedClass,
                expectedMethod,
                expectedFileName,
                expectedLineNumber.ToString())
        });

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e =>
                e.Extra["ClassName"].ToString() == expectedClass
                && e.Extra["FileName"].ToString() == expectedFileName
                && e.Extra["MethodName"].ToString() == expectedMethod
                && (int)e.Extra["LineNumber"] == expectedLineNumber));
    }

    [Fact]
    public void Append_ThreadContext_AsExtra()
    {
        var expected = new object();
        var id = Guid.NewGuid().ToString();
        ThreadContext.Properties[id] = expected;

        var sut = _fixture.GetSut();
        sut.Dsn = "dsn";

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Extra[id] == expected));
    }

    [Fact]
    public void Append_ByDefault_DoesNotSetEnvironment()
    {
        var sut = _fixture.GetSut();
        var evt = new LoggingEvent(new LoggingEventData());

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Environment == null));
    }

    [Fact]
    public void Append_ConfiguredEnvironment()
    {
        const string expected = "dev";
        var sut = _fixture.GetSut();
        sut.Environment = expected;
        var evt = new LoggingEvent(new LoggingEventData());

        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Environment == expected));
    }

    [Fact]
    public void MinimumEventLevel_DefaultsToNull()
    {
        var appender = new SentryAppender();
        Assert.Null(appender.MinimumEventLevel);
    }

    [Fact]
    public void DoAppend_BelowMinimumEventLevel_AddsBreadcrumb()
    {
        var sut = _fixture.GetSut();
        sut.Threshold = Level.Debug;
        sut.MinimumEventLevel = Level.Error;

        const string expectedBreadcrumbMsg = "log4net breadcrumb";
        var warnEvt = new LoggingEvent(new LoggingEventData
        {
            Level = Level.Warn,
            Message = expectedBreadcrumbMsg
        });
        sut.DoAppend(warnEvt);

        var breadcrumb = _fixture.Scope.Breadcrumbs.First();
        Assert.Equal(expectedBreadcrumbMsg, breadcrumb.Message);
    }

    [Fact]
    public void DoAppend_NullMinimumEventLevel_AddsEvent()
    {
        var sut = _fixture.GetSut();
        sut.Threshold = Level.Debug;
        sut.MinimumEventLevel = null;

        const string expectedMessage = "log4net message";
        var warnEvt = new LoggingEvent(new LoggingEventData
        {
            Level = Level.Warn,
            Message = expectedMessage
        });
        sut.DoAppend(warnEvt);

        // No breadcrumb is added.
        Assert.Empty(_fixture.Scope.Breadcrumbs);
        // Event is sent instead.
        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Message == expectedMessage));
    }

    [Fact]
    public void DoAppend_AboveMinimumEventLevel_AddsEvent()
    {
        var sut = _fixture.GetSut();
        sut.Threshold = Level.Debug;
        sut.MinimumEventLevel = Level.Warn;

        const string expectedMessage = "log4net message";
        var warnEvt = new LoggingEvent(new LoggingEventData
        {
            Level = Level.Error,
            Message = expectedMessage
        });
        sut.DoAppend(warnEvt);

        // No breadcrumb is added.
        Assert.Empty(_fixture.Scope.Breadcrumbs);
        // Event is sent instead.
        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Message == expectedMessage));
    }

#nullable enable
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
#nullable restore

    [Fact]
    public void Close_DisposesSdk()
    {
        const string expectedDsn = "dsn";
        var sut = _fixture.GetSut();
        sut.Dsn = expectedDsn;

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        _fixture.SdkDisposeHandle.DidNotReceive().Dispose();

        sut.Close();

        _fixture.SdkDisposeHandle.Received(1).Dispose();
    }

#nullable enable
    private static LoggingEvent CreateLoggingEvent(Level level, string message, Exception? exception = null)
    {
        return new LoggingEvent(null, null, "TestLogger", level, message, exception);
    }

    private static LoggingEvent CreateLoggingEvent(Level level, string format, object[] args)
    {
        var message = new SystemStringFormat(CultureInfo.InvariantCulture, format, args);
        return new LoggingEvent(null, null, "TestLogger", level, message, null);
    }
#nullable restore
}
