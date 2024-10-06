using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggerTests
{
    private class Fixture
    {
        public string CategoryName { get; set; } = "SomeApp";
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public SentryLoggingOptions Options { get; set; } = new();
        public Scope Scope { get; } = new(new SentryOptions());

        public Fixture()
        {
            _ = Hub.IsEnabled.Returns(true);
            Hub.ConfigureScope(Arg.Invoke(Scope));
        }

        public SentryLogger GetSut() => new(CategoryName, Options, new MockClock(), Hub);
    }

    private readonly Fixture _fixture = new();
    private const string BreadcrumbType = null;

    [Fact]
    public void Log_InvokesSdkIsEnabled()
    {
        var sut = _fixture.GetSut();
        sut.Log(LogLevel.Critical, 1, "info", null, null);

        _ = _fixture.Hub.Received(1).IsEnabled;
    }

    [Fact]
    public void Log_EventWithException_NoBreadcrumb()
    {
        var expectedException = new Exception("expected message");

        var sut = _fixture.GetSut();

        // LogLevel.Critical will create an event
        sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

        // Breadcrumbs get created automatically by the hub for captured exceptions... we don't want
        // our logging integration to be creating these also
        _fixture.Scope.Breadcrumbs.Should().BeEmpty();
    }

    [Fact]
    public void Log_EventWithoutException_LeavesBreadcrumb()
    {
        var sut = _fixture.GetSut();

        // LogLevel.Critical will create an event, but there's no exception so we do want a breadcrumb
        sut.Log<object>(LogLevel.Critical, default, null, null, null);

        _fixture.Scope.Breadcrumbs.Should().NotBeEmpty();
    }

    [Fact]
    public void Log_WithEventId_EventIdAsTagOnEvent()
    {
        var expectedEventId = new EventId(10, "EventId-!@#$%^&*(");

        var sut = _fixture.GetSut();

        sut.Log<object>(LogLevel.Critical, expectedEventId, null, null, null);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(
                e => e.Tags[EventIdExtensions.DataKey] == expectedEventId.ToString()));
    }

    [Fact]
    public void Log_WithProperties_SetsTagsInEvent()
    {
        var guidValue = Guid.NewGuid();

        var props = new List<KeyValuePair<string, object>>
        {
            new("fooString", "bar"),
            new("fooInteger", 1234),
            new("fooLong", 1234L),
            new("fooUShort", (ushort)1234),
            new("fooDouble", (double)1234),
            new("fooFloat", (float)1234.123),
            new("fooGuid", guidValue),
            new("fooEnum", UriKind.Absolute) // any enum, just an example
        };
        var sut = _fixture.GetSut();

        sut.Log<object>(LogLevel.Critical, default, props, null, null);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(
                e => e.Tags["fooString"] == "bar" &&
                     e.Tags["fooInteger"] == "1234" &&
                     e.Tags["fooLong"] == "1234" &&
                     e.Tags["fooUShort"] == "1234" &&
                     e.Tags["fooDouble"] == "1234" &&
                     e.Tags["fooFloat"] == "1234.123" &&
                     e.Tags["fooGuid"] == guidValue.ToString() &&
                     e.Tags["fooEnum"] == "Absolute"));
    }

    [Fact]
    public void Culture_does_not_effect_tags()
    {
        var props = new List<KeyValuePair<string, object>>
        {
            new("fooInteger", 12345),
            new("fooDouble", 12345.123d),
            new("fooFloat", 1234.123f),
        };
        SentryEvent sentryEvent;
        var culture = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new("da-DK");
            sentryEvent = SentryLogger.CreateEvent(LogLevel.Debug, default, props, null, null, "category");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }

        var tags = sentryEvent.Tags;
        Assert.Equal("12345", tags["fooInteger"]);
        Assert.Equal("12345.123", tags["fooDouble"]);
        Assert.Equal("1234.123", tags["fooFloat"]);
    }

    [Fact]
    public void Log_WithEmptyGuidProperty_DoesntSetTagInEvent()
    {
        var props = new List<KeyValuePair<string, object>>
        {
            new("fooGuid", Guid.Empty)
        };
        var sut = _fixture.GetSut();

        sut.Log<object>(LogLevel.Critical, default, props, null, null);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(
                e => e.Tags.Count == 0));
    }

    [Fact]
    public void LogCritical_MatchingFilter_DoesNotCapturesEvent()
    {
        const string expected = "message";
        _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
        _fixture.Options.AddLogEntryFilter((_, _, _, _) => true);

        var sut = _fixture.GetSut();

        sut.LogCritical(expected);

        _ = _fixture.Hub.DidNotReceive()
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void LogCritical_MatchingFilter_DoesNotAddBreadcrumb()
    {
        const string expected = "message";
        _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
        _fixture.Options.AddLogEntryFilter((_, _, _, _) => true);

        var sut = _fixture.GetSut();

        sut.LogCritical(expected);

        _fixture.Hub.DidNotReceive()
            // Breadcrumbs live in the scope
            .ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Fact]
    public void LogCritical_NotMatchingFilter_CapturesEvent()
    {
        const string expected = "message";
        _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);
        _fixture.Options.AddLogEntryFilter((_, _, _, _) => false);

        var sut = _fixture.GetSut();

        sut.LogCritical(expected);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void LogCritical_DefaultOptions_CapturesEvent()
    {
        const string expected = "message";
        var sut = _fixture.GetSut();

        sut.LogCritical(expected);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void LogError_DefaultOptions_CapturesEvent()
    {
        const string expected = "message";
        var sut = _fixture.GetSut();

        sut.LogError(expected);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Log_SentryCategory_DoesNotSendEvent()
    {
        var expectedException = new Exception("expected message");
        _fixture.CategoryName = "Sentry.Some.Class";
        var sut = _fixture.GetSut();

        sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

        _ = _fixture.Hub.DidNotReceive()
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void Log_SentryRootCategory_DoesNotSendEvent()
    {
        var expectedException = new Exception("expected message");
        _fixture.CategoryName = "Sentry";
        var sut = _fixture.GetSut();

        sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

        _ = _fixture.Hub.DidNotReceive()
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    // https://github.com/getsentry/sentry-dotnet/issues/132
    [Fact]
    public void Log_SentrySomethingCategory_SendEvent()
    {
        var expectedException = new Exception("expected message");
        _fixture.CategoryName = "SentrySomething";
        var sut = _fixture.GetSut();

        sut.Log<object>(LogLevel.Critical, default, null, expectedException, null);

        _ = _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void LogWarning_DefaultOptions_RecordsBreadcrumbs()
    {
        const string expectedMessage = "message";
        const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Warning;

        var sut = _fixture.GetSut();

        sut.LogWarning(expectedMessage);

        var b = _fixture.Scope.Breadcrumbs.First();
        Assert.Equal(expectedMessage, b.Message);
        Assert.Equal(DateTimeOffset.MaxValue, b.Timestamp);
        Assert.Equal(_fixture.CategoryName, b.Category);
        Assert.Equal(expectedLevel, b.Level);
        Assert.Equal(BreadcrumbType, b.Type);
        Assert.Null(b.Data);
    }

    [Fact]
    public void LogInformation_DefaultOptions_RecordsBreadcrumbs()
    {
        const string expectedMessage = "message";
        const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Info;

        var sut = _fixture.GetSut();

        sut.LogInformation(expectedMessage);

        var b = _fixture.Scope.Breadcrumbs.First();
        Assert.Equal(expectedMessage, b.Message);
        Assert.Equal(DateTimeOffset.MaxValue, b.Timestamp);
        Assert.Equal(_fixture.CategoryName, b.Category);
        Assert.Equal(expectedLevel, b.Level);
        Assert.Equal(BreadcrumbType, b.Type);
        Assert.Null(b.Data);
    }

    [Fact]
    public void LogDebug_DefaultOptions_DoesNotRecordsBreadcrumbs()
    {
        var sut = _fixture.GetSut();

        sut.LogDebug("anything");

        _fixture.Hub.DidNotReceiveWithAnyArgs()
            .AddBreadcrumb(
                default,
                string.Empty,
                default,
                default,
                default,
                default);
    }

    [Fact]
    public void LogTrace_DefaultOptions_DoesNotRecordsBreadcrumbs()
    {
        var sut = _fixture.GetSut();

        sut.LogTrace("anything");

        _fixture.Hub.DidNotReceiveWithAnyArgs()
            .AddBreadcrumb(
                default,
                string.Empty,
                default,
                default,
                default,
                default);
    }

    [Fact]
    public void IsEnabled_DisabledSdk_ReturnsFalse()
    {
        _ = _fixture.Hub.IsEnabled.Returns(false);

        var sut = _fixture.GetSut();

        Assert.False(sut.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void IsEnabled_EnabledSdk_ReturnsTrue()
    {
        _ = _fixture.Hub.IsEnabled.Returns(true);

        var sut = _fixture.GetSut();

        Assert.True(sut.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void IsEnabled_EnabledSdkLogLevelNone_ReturnsFalse()
    {
        _ = _fixture.Hub.IsEnabled.Returns(true);

        var sut = _fixture.GetSut();

        Assert.False(sut.IsEnabled(LogLevel.None));
    }

    [Fact]
    public void IsEnabled_EnabledSdkLogLevelLower_ReturnsFalse()
    {
        _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
        _fixture.Options.MinimumEventLevel = LogLevel.Critical;
        _ = _fixture.Hub.IsEnabled.Returns(true);

        var sut = _fixture.GetSut();

        Assert.False(sut.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void IsEnabled_EnabledSdkLogLevelBreadcrumbLower_ReturnsTrue()
    {
        _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Critical;
        _fixture.Options.MinimumEventLevel = LogLevel.Trace;
        _ = _fixture.Hub.IsEnabled.Returns(true);

        var sut = _fixture.GetSut();

        Assert.True(sut.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void IsEnabled_EnabledSdkLogLevelEventLower_ReturnsTrue()
    {
        _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;
        _fixture.Options.MinimumEventLevel = LogLevel.Critical;
        _ = _fixture.Hub.IsEnabled.Returns(true);

        var sut = _fixture.GetSut();

        Assert.True(sut.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void BeginScope_NullState_PushesScope()
    {
        var sut = _fixture.GetSut();
        _ = sut.BeginScope<object>(null);
        _ = _fixture.Hub.Received(1).PushScope<object>(null);
    }

    [Fact]
    public void BeginScope_StringState_PushesScope()
    {
        const string expected = "state";
        var sut = _fixture.GetSut();
        _ = sut.BeginScope(expected);
        _ = _fixture.Hub.Received(1).PushScope(expected);
    }

    [Fact]
    public void BeginScope_Disposable_Scope()
    {
        var expected = Substitute.For<IDisposable>();
        _ = _fixture.Hub.PushScope(Arg.Any<string>()).Returns(expected);

        var sut = _fixture.GetSut();
        var actual = sut.BeginScope("state");

        Assert.Same(actual, expected);
    }
}
