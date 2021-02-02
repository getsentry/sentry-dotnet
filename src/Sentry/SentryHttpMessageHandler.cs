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
        protected override Task<HttpResponseMessage> SendAsync(
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

            return base.SendAsync(request, cancellationToken);
        }
    }
}
