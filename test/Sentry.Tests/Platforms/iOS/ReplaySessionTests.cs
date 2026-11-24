#if IOS
namespace Sentry.Tests.Platforms.iOS;

public class ReplaySessionTests
{
    [Fact]
    public void ActiveReplayId_NoReplayRecording_ReturnsNull()
    {
        ReplaySession.Instance.ActiveReplayId.Should().BeNull();
    }

    [Fact]
    public void CaptureReplay_NoReplayRecording_ReturnsNull()
    {
        var @event = new SentryEvent { SentryExceptions = [new SentryException { Type = "InvalidOperationException" }] };

        ReplaySession.Instance.CaptureReplay(@event).Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptureCocoaReplay_ReplayOff_ReturnsNullWithoutDispatch(bool withOptions)
    {
        var options = withOptions ? new SentryOptions() : null;
        var dispatched = false;

        var result = ((ReplaySession)ReplaySession.Instance).CaptureCocoaReplay(options, isMainThread: false,
            _ => dispatched = true);

        result.Should().BeNull();
        dispatched.Should().BeFalse();
    }
}
#endif
