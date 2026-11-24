#if ANDROID
namespace Sentry.Tests.Platforms.Android;

public class ReplaySessionTests
{
    [Fact]
    public void JavaScopesReadReplayId_NoReplayRecording_ReturnsNull()
    {
        ReplaySession.JavaScopes.ReadReplayId().Should().BeNull();
    }
}
#endif
