using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http
{
    internal class HttpTransport : ITransport
    {
        private readonly SentryOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ISystemClock _clock = new SystemClock();

        // Keep track of rate limits and their expiry dates
        private readonly Dictionary<RateLimitCategory, DateTimeOffset> _categoryLimitResets =
            new();

        internal const string DefaultErrorMessage = "No message";

        public HttpTransport(SentryOptions options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
        }

        private Envelope ProcessEnvelope(Envelope envelope, DateTimeOffset instant)
        {
            // Re-package envelope, discarding items that don't fit the rate limit
            var envelopeItems = new List<EnvelopeItem>();
            foreach (var envelopeItem in envelope.Items)
            {
                // Check if there is at least one matching category for this item that is rate-limited
                var isRateLimited = _categoryLimitResets
                    .Any(kvp => kvp.Value > instant && kvp.Key.Matches(envelopeItem));

                if (isRateLimited)
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Envelope item of type {0} was discarded because it's rate-limited.",
                        envelopeItem.TryGetType()
                    );

                    continue;
                }

                // If attachment, needs to respect attachment size limit
                if (string.Equals(envelopeItem.TryGetType(), "attachment", StringComparison.OrdinalIgnoreCase) &&
                    envelopeItem.TryGetLength() > _options.MaxAttachmentSize)
                {
                    _options.DiagnosticLogger?.LogWarning(
                        "Attachment '{0}' dropped because it's too large ({1} bytes).",
                        envelopeItem.TryGetFileName(),
                        envelopeItem.TryGetLength()
                    );

                    continue;
                }

                envelopeItems.Add(envelopeItem);
            }

            return new Envelope(envelope.Header, envelopeItems);
        }

        private void ExtractRateLimits(HttpResponseMessage response, DateTimeOffset instant)
        {
            if (!response.Headers.TryGetValues("X-Sentry-Rate-Limits", out var rateLimitHeaderValues))
            {
                return;
            }

            // Join to a string to handle both single-header and multi-header cases
            var rateLimitsEncoded = string.Join(",", rateLimitHeaderValues);

            // Parse and order by retry-after so that the longer rate limits are set last (and not overwritten)
            var rateLimits = RateLimit.ParseMany(rateLimitsEncoded).OrderBy(rl => rl.RetryAfter);

            // Persist rate limits
            foreach (var rateLimit in rateLimits)
            {
                foreach (var rateLimitCategory in rateLimit.Categories)
                {
                    _categoryLimitResets[rateLimitCategory] = instant + rateLimit.RetryAfter;
                }
            }
        }

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            var instant = DateTimeOffset.Now;

            // Apply rate limiting and re-package envelope items
            using var processedEnvelope = ProcessEnvelope(envelope, instant);
            if (processedEnvelope.Items.Count == 0)
            {
                _options.DiagnosticLogger?.LogInfo(
                    "Envelope {0} was discarded because all contained items are rate-limited.",
                    envelope.TryGetEventId()
                );

                return;
            }

            // Send envelope to ingress
            using var request = CreateRequest(processedEnvelope);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Read & set rate limits for future requests
            ExtractRateLimits(response, instant);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Envelope {0} successfully received by Sentry.",
                    processedEnvelope.TryGetEventId()
                );
            }
            else if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) == true)
            {
                if (string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                {
                    var responseJson = await response.Content.ReadAsJsonAsync(cancellationToken).ConfigureAwait(false);

                    var errorMessage =
                        responseJson.GetPropertyOrNull("detail")?.GetString()
                        ?? DefaultErrorMessage;

                    var errorCauses =
                        responseJson.GetPropertyOrNull("causes")?.EnumerateArray().Select(j => j.GetString()).ToArray()
                        ?? Array.Empty<string>();

                    _options.DiagnosticLogger?.Log(
                        SentryLevel.Error,
                        "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}. Error causes: {3}.",
                        null,
                        processedEnvelope.TryGetEventId(),
                        response.StatusCode,
                        errorMessage,
                        string.Join(", ", errorCauses)
                    );
                }
                else
                {
                    var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    _options.DiagnosticLogger?.Log(
                        SentryLevel.Error,
                        "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}.",
                        null,
                        processedEnvelope.TryGetEventId(),
                        response.StatusCode,
                        responseString
                    );
                }
            }
        }

        internal HttpRequestMessage CreateRequest(Envelope envelope)
        {
            if (string.IsNullOrWhiteSpace(_options.Dsn))
            {
                throw new InvalidOperationException("The DSN is expected to be set at this point.");
            }

            var dsn = Dsn.Parse(_options.Dsn);

            var authHeader =
                $"Sentry sentry_version={_options.SentryVersion}," +
                $"sentry_client={_options.ClientVersion}," +
                $"sentry_key={dsn.PublicKey}," +
                (dsn.SecretKey is { } secretKey ? $"sentry_secret={secretKey}," : null) +
                $"sentry_timestamp={_clock.GetUtcNow().ToUnixTimeSeconds()}";

            return new HttpRequestMessage
            {
                RequestUri = dsn.GetEnvelopeEndpointUri(),
                Method = HttpMethod.Post,
                Headers = {{"X-Sentry-Auth", authHeader}},
                Content = new EnvelopeHttpContent(envelope)
            };
        }
    }
}
