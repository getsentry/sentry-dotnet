namespace Sentry.Tests;

public class SentryClientExtensionsTests
{
    private readonly ISentryClient _sut = Substitute.For<ISentryClient>();

    [Fact]
    public void CaptureException_DisabledClient_DoesNotCaptureEvent()
    {
        _ = _sut.IsEnabled.Returns(false);
        var id = _sut.CaptureException(new Exception());

        _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal(default, id);
    }

    [Fact]
    public void CaptureException_EnabledClient_CapturesEvent()
    {
        _ = _sut.IsEnabled.Returns(true);
        _ = _sut.CaptureException(new Exception());
        _ = _sut.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureMessage_DisabledClient_DoesNotCaptureEvent()
    {
        _ = _sut.IsEnabled.Returns(false);
        var id = _sut.CaptureMessage("Message");

        _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal(default, id);
    }

    [Fact]
    public void CaptureMessage_EnabledClient_CapturesEvent()
    {
        _ = _sut.IsEnabled.Returns(true);
        _ = _sut.CaptureMessage("Message");
        _ = _sut.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureMessage_Level_CapturesEventWithLevel()
    {
        const SentryLevel expectedLevel = SentryLevel.Fatal;
        _ = _sut.IsEnabled.Returns(true);
        _ = _sut.CaptureMessage("Message", expectedLevel);
        _ = _sut.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == expectedLevel));
    }

    [Fact]
    public void CaptureMessage_Message_CapturesEventWithMessage()
    {
        const string expectedMessage = "message";
        _ = _sut.IsEnabled.Returns(true);
        _ = _sut.CaptureMessage(expectedMessage);
        _ = _sut.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Message == expectedMessage));
    }

    [Fact]
    public void CaptureMessage_WhitespaceMessage_DoesNotCapturesEventWithMessage()
    {
        _ = _sut.IsEnabled.Returns(true);
        var id = _sut.CaptureMessage("   ");

        _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal(default, id);
    }

    [Fact]
    public void CaptureMessage_NullMessage_DoesNotCapturesEventWithMessage()
    {
        _ = _sut.IsEnabled.Returns(true);
        var id = _sut.CaptureMessage(null!);

        _ = _sut.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal(default, id);
    }

    [Fact]
    public void CaptureUserFeedback_EnabledClient_CapturesUserFeedback()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _ = _sut.IsEnabled.Returns(true);
        _sut.CaptureUserFeedback(Guid.Parse("1ec19311a7c048818de80b18dcc43eaa"), "email@email.com", "comments");
        _sut.Received(1).CaptureUserFeedback(Arg.Any<UserFeedback>());
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void CaptureUserFeedback_DisabledClient_DoesNotCaptureUserFeedback()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _ = _sut.IsEnabled.Returns(false);
        _sut.CaptureUserFeedback(Guid.Parse("1ec19311a7c048818de80b18dcc43eea"), "email@email.com", "comments");

        _sut.DidNotReceive().CaptureUserFeedback(Arg.Any<UserFeedback>());
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public async Task FlushAsync_NoTimeoutSpecified_UsesFlushTimeoutFromOptions()
    {
        var timeout = TimeSpan.FromSeconds(12345);
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions
        {
            FlushTimeout = timeout
        };

        await _sut.FlushAsync();

        await _sut.Received(1).FlushAsync(timeout);
    }

    [Fact]
    public async Task Flush_NoTimeoutSpecified_UsesFlushTimeoutFromOptions()
    {
        var timeout = TimeSpan.FromSeconds(12345);
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions
        {
            FlushTimeout = timeout
        };

        // ReSharper disable once MethodHasAsyncOverload
        _sut.Flush();

        await _sut.Received(1).FlushAsync(timeout);
    }

    [Fact]
    public async Task Flush_WithTimeoutSpecified_UsesThatTimeout()
    {
        var timeout = TimeSpan.FromSeconds(12345);

        // ReSharper disable once MethodHasAsyncOverload
        _sut.Flush(timeout);

        await _sut.Received(1).FlushAsync(timeout);
    }
}
