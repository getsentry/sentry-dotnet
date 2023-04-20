using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry
{
    internal class SentryFailedRequestHandler : ISentryFailedRequestHandler
    {
        private readonly IHub _hub;
        private readonly SentryOptions? _options;

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

            // Don't capture successful requets
            if (_options?.FailedRequestStatusCodes.Any(range => range.Contains(response.StatusCode)) is false)
                return;

            /*
            Note the Sentry Java SDK strips the query string and fragment from the URL. However that limits
            how this feature can be used. If the user wants to ignore the query string and fragment, they can
            do so explicitly via regex pattern matching. We've left the query/fragment entact in this SDK.
            */
            var uri = request.RequestUri;
            var requestString = uri?.OriginalString ?? "";

            // Ignore requests to the Sentry DSN
            if (_options?.Dsn is { } dsn && new SubstringOrRegexPattern(dsn).IsMatch(requestString))
                return;

            // Only capture requets matching the FailedRequestTargets
            if (_options?.FailedRequestTargets.ContainsMatch(requestString) is false)
                return;

            // Capture the event
            throw new NotImplementedException();

            //_hub.CaptureEvent()


            /*
            val exception = SentryHttpClientException(
                "HTTP Client Error with status code: ${response.code}"
            )
            val mechanismException = ExceptionMechanismException(mechanism, exception, Thread.currentThread(), true)
            val event = SentryEvent(mechanismException)

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
