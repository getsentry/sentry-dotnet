namespace Sentry.Testing;

internal class FakeFailingTransport : ITransport
{
    public Task SendEnvelopeAsync(
        Envelope envelope,
        CancellationToken cancellationToken = default)
    {
        throw new Exception("Expected transport failure has occured.")
        {
            Source = nameof(FakeFailingTransport)
        };
    }
}
