namespace Sentry.Log4Net.Tests;

public partial class SentryAppenderTests : IDisposable
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Scope Scope { get; } = new(new SentryOptions());
        public SentryOptions Options { get; } = new();

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
            Hub.SubstituteConfigureScope(Scope);
        }

        public SentryAppender GetSut()
        {
            SentryClientExtensions.SentryOptionsForTestingOnly = Options;

            var sut = new SentryAppender(Hub);
            sut.ActivateOptions();
            return sut;
        }
    }

    private readonly Fixture _fixture = new();

    public void Dispose()
    {
        SentryClientExtensions.SentryOptionsForTestingOnly = null;
        // ThreadContext.Properties is thread-static and can leak into subsequent tests running on the same thread.
        ThreadContext.Properties.Clear();
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
    public void Append_DisabledHub_HubNotCalled()
    {
        _ = _fixture.Hub.IsEnabled.Returns(false);
        InMemorySentryStructuredLogger capturer = new();
        _fixture.Hub.Logger.Returns(capturer);
        var sut = _fixture.GetSut();

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        _ = _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(null);
        capturer.Logs.Should().BeEmpty();
        _fixture.Scope.Breadcrumbs.Should().BeEmpty();
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

        var evt = new LoggingEvent(new LoggingEventData());
        sut.DoAppend(evt);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Extra[id] == expected));
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
}
