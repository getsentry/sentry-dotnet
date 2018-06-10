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

namespace Sentry.Internal.Http
{
    internal class HttpTransport : ITransport
    {
        private readonly HttpOptions _options;
        private readonly HttpClient _httpClient;
        private readonly Action<HttpRequestHeaders> _addAuth;

        public HttpTransport(
            HttpOptions options,
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
#if DEBUG
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseId = JsonSerializer.DeserializeObject<SentrySuccessfulResponseBody>(body)?.id;
                Debug.Assert(@event.EventId.ToString("N") == responseId);
#else
                return;
#endif
            }
            else if (
                _options.HandleFailedEventSubmission != null &&
                response.Headers.TryGetValues(SentryHeaders.SentryErrorHeader, out var values))
            {
                var errorMessage = values.FirstOrDefault() ?? "No message";
                _options.HandleFailedEventSubmission?.Invoke(@event, response.StatusCode, errorMessage);
            }
        }

        private HttpRequestMessage CreateRequest(SentryEvent @event)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = _options.SentryUri,
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.SerializeObject(@event))
            };

            _addAuth(request.Headers);
            return request;
        }
    }
}
