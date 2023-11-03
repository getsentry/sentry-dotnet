using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

internal class LazyHttpTransport : ITransport
{
    private readonly SentryOptions _options;
    private HttpTransport? _httpTransport;

    public LazyHttpTransport(SentryOptions options)
    {
        _options = options;
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        _httpTransport ??= new HttpTransport(_options, (_options.SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory()).Create(_options));

        return _httpTransport.SendEnvelopeAsync(envelope, cancellationToken);
    }
}
