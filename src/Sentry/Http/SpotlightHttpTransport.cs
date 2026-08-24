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
    private readonly Uri _spotlightUrl;
    private readonly ISystemClock _clock;
    private readonly ExponentialBackoff _backoff;

    public SpotlightHttpTransport(ITransport inner, SentryOptions options, HttpClient httpClient, Uri spotlightUrl, ISystemClock clock)
        : base(options, httpClient)
    {
        _options = options;
        _httpClient = httpClient;
        _spotlightUrl = spotlightUrl;
        _inner = inner;
        _clock = clock;
        _backoff = new ExponentialBackoff(clock);
    }

    protected internal override HttpRequestMessage CreateRequest(Envelope envelope)
    {
        return new HttpRequestMessage
        {
            RequestUri = _spotlightUrl,
            Method = HttpMethod.Post,
            Content = new EnvelopeHttpContent(envelope, _options.DiagnosticLogger, _clock)
            { Headers = { ContentType = MediaTypeHeaderValue.Parse("application/x-sentry-envelope") } }
        };
    }

    public override async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        var sentryTask = _inner.SendEnvelopeAsync(envelope, cancellationToken);

        if (_backoff.ShouldAttempt())
        {
            try
            {
                // Send to spotlight
                using var processedEnvelope = ProcessEnvelope(envelope);
                if (processedEnvelope.Items.Count > 0)
                {
                    using var request = CreateRequest(processedEnvelope);
                    using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    await HandleResponseAsync(response, processedEnvelope, cancellationToken).ConfigureAwait(false);

                    _backoff.RecordSuccess();
                }
            }
            catch (Exception e)
            {
                int failureCount = _backoff.RecordFailure();
                if (failureCount == 1)
                {
                    _options.LogError(e, "Failed sending envelope to Spotlight.");
                }
            }
        }

        // await the Sentry request before returning
        await sentryTask.ConfigureAwait(false);
    }

    private class ExponentialBackoff
    {
        private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(60);

        private readonly ISystemClock _clock;

        private readonly Lock _lock = new();
        private TimeSpan _retryDelay = InitialRetryDelay;
        private DateTimeOffset _retryAfter = DateTimeOffset.MinValue;
        private int _failureCount;

        public ExponentialBackoff(ISystemClock clock)
        {
            _clock = clock;
        }

        public bool ShouldAttempt()
        {
            lock (_lock)
            {
                return _clock.GetUtcNow() >= _retryAfter;
            }
        }

        public int RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _retryAfter = _clock.GetUtcNow() + _retryDelay;
                _retryDelay = TimeSpan.FromTicks(Math.Min(_retryDelay.Ticks * 2, MaxRetryDelay.Ticks));
                return _failureCount;
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _retryDelay = InitialRetryDelay;
                _retryAfter = DateTimeOffset.MinValue;
            }
        }
    }
}
