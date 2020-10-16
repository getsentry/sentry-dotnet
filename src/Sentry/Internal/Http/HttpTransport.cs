using System;
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
            var request = CreateRequest(envelope);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Envelope {0} successfully received by Sentry.",
                    envelope.TryGetEventId()
                );
            }
            else if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) == true)
            {
                var responseJson = await response.Content.ReadAsJsonAsync();
                var errorMessage = responseJson.SelectToken("detail")?.Value<string>() ?? NoMessageFallback;

                _options.DiagnosticLogger?.Log(
                    SentryLevel.Error,
                    "Sentry rejected the envelope {0}. Status code: {1}. Sentry response: {2}",
                    null,
                    envelope.TryGetEventId(),
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
