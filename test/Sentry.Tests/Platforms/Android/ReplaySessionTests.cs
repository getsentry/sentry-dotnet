#if ANDROID
namespace Sentry.Tests.Platforms.Android;

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
}
#endif
