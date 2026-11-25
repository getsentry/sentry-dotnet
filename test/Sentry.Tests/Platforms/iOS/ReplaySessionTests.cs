#if IOS
namespace Sentry.Tests.Platforms.iOS;

public class ReplaySessionTests
{
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
