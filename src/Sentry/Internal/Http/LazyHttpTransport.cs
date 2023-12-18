using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

internal class LazyHttpTransport : ITransport
{
    private readonly Lazy<HttpTransport> _httpTransport;

    public LazyHttpTransport(SentryOptions options)
    {
        _httpTransport = new Lazy<HttpTransport>(() => new HttpTransport(options, options.GetHttpClient()));
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        return _httpTransport.Value.SendEnvelopeAsync(envelope, cancellationToken);
    }
}
