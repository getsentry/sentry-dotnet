#if !__MOBILE__
namespace Sentry.Tests;

[UsesVerify]
public partial class SentryClientTests
{
    [Fact]
    public Task CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
    {
        var error = new Exception("Exception message!");
        _fixture.SentryOptions.BeforeSend = _ => throw error;

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        return Verify(@event.Breadcrumbs);
    }
}
#endif
