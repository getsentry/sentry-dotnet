using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry
{
    /// <summary>
    /// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
    /// </summary>
    public class SentryHttpMessageHandler : DelegatingHandler
    {
        private readonly IHub _hub;

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler(IHub hub)
        {
            _hub = hub;
        }

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub)
            : this(hub)
        {
            InnerHandler = innerHandler;
        }

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler(HttpMessageHandler innerHandler)
            : this(innerHandler, HubAdapter.Instance) {}

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler()
            : this(HubAdapter.Instance) {}

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Set trace header if it hasn't already been set
            if (!request.Headers.Contains(SentryTraceHeader.HttpHeaderName) &&
                _hub.GetTraceHeader() is {} traceHeader)
            {
                request.Headers.Add(
                    SentryTraceHeader.HttpHeaderName,
                    traceHeader.ToString()
                );
            }

            // Prevent null reference exception in the following call
            // in case the user didn't set an inner handler.
            InnerHandler ??= new HttpClientHandler();

            // Start a span that tracks this request
            // (may be null if transaction is not set on the scope)
            var span = _hub.GetSpan()?.StartChild(
                "http.client",
                // e.g. "GET https://example.com"
                $"{request.Method.Method.ToUpperInvariant()} {request.RequestUri}"
            );

            try
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                // This will handle unsuccessful status codes as well
                span?.Finish(
                    SpanStatusConverter.FromHttpStatusCode(response.StatusCode)
                );

                return response;
            }
            catch
            {
                // TODO: attach the exception to the span, once
                // that API is available.
                span?.Finish(SpanStatus.UnknownError);

                throw;
            }
        }
    }
}
