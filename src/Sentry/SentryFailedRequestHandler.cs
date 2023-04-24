namespace Sentry
{
    internal class SentryFailedRequestHandler : ISentryFailedRequestHandler
    {
        private readonly IHub _hub;
        private readonly SentryOptions _options;

        public const string MechanismType = "SentryFailedRequestHandler";

        /// <summary>
        /// Initializes an instance of <see cref="SentryFailedRequestHandler"/>.
        /// </summary>
        internal SentryFailedRequestHandler(IHub hub, SentryOptions? options)
        {
            _hub = hub;
            _options = options;
        }

        /// <summary>
        /// Automaticlaly capture HTTP Client errors when captureFailedRequests
        /// </summary>
        /// <param name="request">The HttpRequestMessage sent</param>
        /// <param name="response">The HttpResponse received</param>
        public void CaptureEvent(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (request is null)
                return;

            // Don't capture if the option is disabled
            if (_options?.CaptureFailedRequests is false)
                return;

            // Don't capture events for successful requets
            if (_options?.FailedRequestStatusCodes.Any(range => range.Contains(response.StatusCode)) is false)
                return;

            // Ignore requests to the Sentry DSN
            var uri = response.RequestMessage.RequestUri;
            if (_options.Dsn is { } dsn && new Uri(dsn).Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
                return;

            // Ignore requests that don't match the FailedRequestTargets
            if (_options?.FailedRequestTargets.ContainsMatch(requestString) is false)
                return;

            // Capture the event
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException exception)
            {
                exception.SetSentryMechanism(MechanismType);
                _hub.CaptureException(exception);
            }

            /*
             * Copied from SentryOkHttpInterceptorTest.kt in the Java SDK for reference

            val hint = Hint()
            hint.set(OKHTTP_REQUEST, request)
            hint.set(OKHTTP_RESPONSE, response)

            val sentryRequest = io.sentry.protocol.Request().apply {
                url = requestUrl
                // Cookie is only sent if isSendDefaultPii is enabled
                cookies = if (hub.options.isSendDefaultPii) request.headers["Cookie"] else null
                method = request.method
                queryString = query
                headers = getHeaders(request.headers)
                fragment = urlFragment

                request.body?.contentLength().ifHasValidLength {
                    bodySize = it
                }
            }

            val sentryResponse = io.sentry.protocol.Response().apply {
                // Cookie is only sent if isSendDefaultPii is enabled due to PII
                cookies = if (hub.options.isSendDefaultPii) response.headers["Cookie"] else null
                headers = getHeaders(response.headers)
                statusCode = response.code

                response.body?.contentLength().ifHasValidLength {
                    bodySize = it
                }
            }

            event.request = sentryRequest
            event.contexts.setResponse(sentryResponse)

            hub.captureEvent(event, hint)
        } 
             */
        }
    }
}
