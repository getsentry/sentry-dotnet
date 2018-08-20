using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Protocol;

namespace Sentry.Internal.Http
{
    internal class HttpTransport : ITransport
    {
        private readonly SentryOptions _options;
        private readonly HttpClient _httpClient;
        private readonly Action<HttpRequestHeaders> _addAuth;

        internal const string NoMessageFallback = "No message";

        public HttpTransport(
            SentryOptions options,
            HttpClient httpClient,
            Action<HttpRequestHeaders> addAuth)
        {
            Debug.Assert(options != null);
            Debug.Assert(httpClient != null);
            Debug.Assert(addAuth != null);

            _options = options;
            _httpClient = httpClient;
            _addAuth = addAuth;
        }

        public async Task CaptureEventAsync(SentryEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event == null)
            {
                return;
            }

            var request = CreateRequest(@event);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _options.DiagnosticLogger?.LogDebug("Event {0} successfully received by Sentry.", @event.EventId);
#if DEBUG
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseId = JsonSerializer.DeserializeObject<SentrySuccessfulResponseBody>(body)?.id;
                Debug.Assert(@event.EventId.ToString("N") == responseId);
#endif
                return;
            }

            if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) == true)
            {
                response.Headers.TryGetValues(SentryHeaders.SentryErrorHeader, out var values);
                var errorMessage = values?.FirstOrDefault() ?? NoMessageFallback;
                _options.DiagnosticLogger?.Log(SentryLevel.Error, "Sentry rejected the event {0}. Status code: {1}. Sentry response: {2}", null,
                    @event.EventId, response.StatusCode, errorMessage);
            }
        }

        internal HttpRequestMessage CreateRequest(SentryEvent @event)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = _options.Dsn.SentryUri,
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.SerializeObject(@event))
            };

            _addAuth(request.Headers);
            return request;
        }
    }
}
