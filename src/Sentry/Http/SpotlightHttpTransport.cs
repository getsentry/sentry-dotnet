using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;

namespace Sentry.Http;

internal class SpotlightHttpTransport : HttpTransport
{
    private readonly ITransport _inner;
    private readonly SentryOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ISystemClock _clock;

    public SpotlightHttpTransport(ITransport inner, SentryOptions options, HttpClient httpClient, ISystemClock clock)
        : base(options, httpClient)
    {
        _options = options;
        _httpClient = httpClient;
        _inner = inner;
        _clock = clock;
    }

    protected internal override HttpRequestMessage CreateRequest(Envelope envelope)
    {
        if (!Uri.TryCreate(_options.SpotlightUrl,  UriKind.Absolute, out var spotlightUrl))
        {
            throw new InvalidOperationException("Invalid option for SpotlightUrl: " + _options.SpotlightUrl);
        }

        return new HttpRequestMessage
        {
            RequestUri = spotlightUrl,
            Method = HttpMethod.Post,
            Content = new EnvelopeHttpContent(envelope, _options.DiagnosticLogger, _clock)
            { Headers = { ContentType = MediaTypeHeaderValue.Parse("application/x-sentry-envelope") } }
        };
    }

    public override async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        var sentryTask = _inner.SendEnvelopeAsync(envelope, cancellationToken);

        try
        {
            // Send to spotlight
            using var processedEnvelope = ProcessEnvelope(envelope);
            if (processedEnvelope.Items.Count > 0)
            {
                using var request = CreateRequest(processedEnvelope);
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HandleResponseAsync(response, processedEnvelope, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed sending envelope to Spotlight.");
        }

        // await the Sentry request before returning
        await sentryTask.ConfigureAwait(false);
    }
}
