using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
    options.Dsn = "http://key@127.0.0.1:9999/123";
    options.Debug = true;
    options.Transport = new FakeTransport();
});

throw new ApplicationException("Something happened!");

internal class FakeTransport : ITransport
{
    public virtual Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        envelope.Serialize(Console.OpenStandardOutput(), null);
        return Task.CompletedTask;
    }
}
