using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

internal class LazyHttpTransport : ITransport
{
    private readonly Lazy<HttpTransport> _httpTransport;

    public LazyHttpTransport(SentryOptions options)
    {
        _httpTransport = new Lazy<HttpTransport>(() =>
        {
            var factory = (options.SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory()).Create(options);
            return new HttpTransport(options, factory);
        });
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        return _httpTransport.Value.SendEnvelopeAsync(envelope, cancellationToken);
    }
}
