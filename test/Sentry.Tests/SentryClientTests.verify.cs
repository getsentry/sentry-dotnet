namespace Sentry.Tests;

[UsesVerify]
public partial class SentryClientTests
{
    [Fact]
    public Task CaptureEvent_BeforeEventThrows_ErrorToEventBreadcrumb()
    {
        // Arrange
        var error = new Exception("Exception message!");
        _fixture.SentryOptions.SetBeforeSend((_,_) => throw error);
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();

        // Act
        _ = sut.CaptureEvent(@event);

        // Assert
        return Verify(@event.Breadcrumbs);
    }

    [Fact]
    public Task CaptureTransaction_BeforeSendTransactionThrows_ErrorToEventBreadcrumb()
    {
        // Arrange
        var error = new Exception("Exception message!");
        _fixture.SentryOptions.SetBeforeSendTransaction((_, _) => throw error);
        var transaction = new Transaction("name", "operation")
        {
            IsSampled = true
        };
        var sut = _fixture.GetSut();

        // Act
        sut.CaptureTransaction(transaction);

        // Assert
        return Verify(transaction.Breadcrumbs);
    }
}
