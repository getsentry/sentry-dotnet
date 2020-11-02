using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Internal.Http
{
    internal class HttpTransport : ITransport
    {
        private readonly SentryOptions _options;
        private readonly HttpClient _httpClient;
        private readonly Action<HttpRequestHeaders> _addAuth;

        // Keep track of rate limits and their expiry dates
        private readonly Dictionary<RateLimitCategory, DateTimeOffset> _categoryLimitResets =
            new Dictionary<RateLimitCategory, DateTimeOffset>();

        internal const string DefaultErrorMessage = "No message";

        public HttpTransport(
            SentryOptions options,
            HttpClient httpClient,
            Action<HttpRequestHeaders> addAuth)
        {
            _options = options;
            _httpClient = httpClient;
            _addAuth = addAuth;
        }

        private Envelope ApplyRateLimitsOnEnvelope(Envelope envelope, DateTimeOffset instant)
        {
            // Re-package envelope, discarding items that don't fit the rate limit
            var envelopeItems = new List<EnvelopeItem>();
            foreach (var envelopeItem in envelope.Items)
            {
                // Check if there is at least one matching category for this item that is rate-limited
                var isRateLimited = _categoryLimitResets
                    .Where(kvp => kvp.Value > instant)
                    .Any(kvp => kvp.Key.Matches(envelopeItem));

                if (!isRateLimited)
                {
                    envelopeItems.Add(envelopeItem);
                }
                else
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Envelope item of type {0} was discarded because it's rate-limited.",
                        envelopeItem.TryGetType()
                    );
                }
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

        public async ValueTask SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            var instant = DateTimeOffset.Now;

            // Apply rate limiting and re-package envelope items
            using var processedEnvelope = ApplyRateLimitsOnEnvelope(envelope, instant);
            if (!processedEnvelope.Items.Any())
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
                var responseJson = await response.Content.ReadAsJsonAsync().ConfigureAwait(false);
                var errorMessage = responseJson.SelectToken("detail")?.Value<string>() ?? DefaultErrorMessage;

                _options.DiagnosticLogger?.Log(
                    SentryLevel.Error,
                    "Sentry rejected the envelope {0}. Status code: {1}. Sentry response: {2}",
                    null,
                    processedEnvelope.TryGetEventId(),
                    response.StatusCode,
                    errorMessage
                );
            }
        }

        internal HttpRequestMessage CreateRequest(Envelope envelope)
        {
            if (string.IsNullOrWhiteSpace(_options.Dsn))
            {
                throw new InvalidOperationException("The DSN is expected to be set at this point.");
            }

            var dsn = Dsn.Parse(_options.Dsn);

            var request = new HttpRequestMessage
            {
                RequestUri = dsn.GetEnvelopeEndpointUri(),
                Method = HttpMethod.Post,
                Content = new SerializableHttpContent(envelope)
            };

            _addAuth(request.Headers);
            return request;
        }
    }
}
