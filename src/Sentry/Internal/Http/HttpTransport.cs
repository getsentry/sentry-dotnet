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

        // Envelope item type -> rate limit reset mapping
        private readonly Dictionary<string, DateTimeOffset> _quotaLimitResets =
            new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);

        internal const string NoMessageFallback = "No message";

        public HttpTransport(
            SentryOptions options,
            HttpClient httpClient,
            Action<HttpRequestHeaders> addAuth)
        {
            _options = options;
            _httpClient = httpClient;
            _addAuth = addAuth;
        }

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            var instant = DateTimeOffset.Now;

            // Discard envelope items if they don't fit in the quota limit
            var envelopeItems = new List<EnvelopeItem>();
            foreach (var envelopeItem in envelope.Items)
            {
                var category = envelopeItem.TryGetType();

                if (!string.IsNullOrWhiteSpace(category) &&
                    _quotaLimitResets.TryGetValue(category, out var resetInstant))
                {
                    if (instant > resetInstant)
                    {
                        envelopeItems.Add(envelopeItem);
                    }
                }
                else
                {
                    envelopeItems.Add(envelopeItem);
                }
            }

            if (!envelopeItems.Any())
            {
                _options.DiagnosticLogger?.LogInformation(
                    "Envelope {0} was discarded because all contained items are rate-limited.",
                    envelope.TryGetEventId()
                );
                return;
            }

            // TODO: optimize/refactor
            var actualEnvelope = new Envelope(envelope.Header, envelopeItems);

            var request = CreateRequest(actualEnvelope);
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Read & set quota limits for future
            if (response.Headers.TryGetValues("X-Sentry-Rate-Limits", out var quotaLimitValues))
            {
                var quotaLimits = quotaLimitValues.Select(SentryEnvelopeQuotaLimit.Parse).ToArray();

                foreach (var quotaLimit in quotaLimits)
                {
                    foreach (var quotaLimitCategory in quotaLimit.Categories)
                    {
                        _quotaLimitResets[quotaLimitCategory] = instant + quotaLimit.RetryAfter;
                    }
                }
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Envelope {0} successfully received by Sentry.",
                    actualEnvelope.TryGetEventId()
                );
            }
            else if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) == true)
            {
                var responseJson = await response.Content.ReadAsJsonAsync().ConfigureAwait(false);
                var errorMessage = responseJson.SelectToken("detail")?.Value<string>() ?? NoMessageFallback;

                _options.DiagnosticLogger?.Log(
                    SentryLevel.Error,
                    "Sentry rejected the envelope {0}. Status code: {1}. Sentry response: {2}",
                    null,
                    actualEnvelope.TryGetEventId(),
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
