using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Http;

/// <summary>
/// A standalone transport that sends pre-serialized envelopes to a Spotlight sidecar.
/// This transport is independent of the main Sentry transport — it does not wrap or delegate to it.
/// Serialization happens upstream (in <see cref="SentryClient.SendToSpotlight"/>) on the calling
/// thread, so this transport only handles the HTTP POST and backoff logic.
/// </summary>
internal class SpotlightHttpTransport : ISpotlightTransport
{
    private readonly SentryOptions _options;
    private readonly HttpClient _httpClient;
    private readonly Uri _spotlightUrl;
    internal readonly ExponentialBackoff _backoff; // internal for testing

    public SpotlightHttpTransport(SentryOptions options, HttpClient httpClient, Uri spotlightUrl, ISystemClock clock)
    {
        _options = options;
        _httpClient = httpClient;
        _spotlightUrl = spotlightUrl;
        _backoff = new ExponentialBackoff(clock);
    }

    public async Task SendAsync(byte[] serializedEnvelope, CancellationToken cancellationToken = default)
    {
        if (!_backoff.ShouldAttempt())
        {
            return;
        }

        try
        {
            using var content = new ByteArrayContent(serializedEnvelope);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-sentry-envelope");

            using var request = new HttpRequestMessage
            {
                RequestUri = _spotlightUrl,
                Method = HttpMethod.Post,
                Content = content
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            _backoff.RecordSuccess();
        }
        catch (Exception e)
        {
            var failureCount = _backoff.RecordFailure();
            if (failureCount == 1)
            {
                _options.LogError(e, "Failed sending envelope to Spotlight at {0}.", _spotlightUrl);
            }
        }
    }

    internal class ExponentialBackoff
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
