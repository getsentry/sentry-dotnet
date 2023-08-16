namespace Sentry.Tests;

[UsesVerify]
public partial class SentryClientTests
{
    [Fact]
    public Task CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
    {
        var error = new Exception("Exception message!");
        _fixture.SentryOptions.SetBeforeSend((_,_) => throw error);

        var @event = new SentryEvent();

        var sut = _fixture.GetSut();
        _ = sut.CaptureEvent(@event);

        return Verify(@event.Breadcrumbs);
    }

    [Fact]
    public Task CaptureTransaction_BeforeSendTransactionThrows_ErrorToEventBreadcrumb()
    {
        var error = new Exception("Exception message!");
        _fixture.SentryOptions.SetBeforeSendTransaction((_, _) => throw error);

        var transaction = new Transaction("name", "operation")
        {
            IsSampled = true
        };

        var sut = _fixture.GetSut();
        sut.CaptureTransaction(transaction);

        return Verify(transaction.Breadcrumbs);
    }
}
