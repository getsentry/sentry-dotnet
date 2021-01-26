using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Special HTTP message handler that can be used to propagate Sentry headers and other contextual information.
    /// </summary>
    public class SentryHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _httpMessageInvoker;
        private readonly IHub _hub;

        private SentryHttpMessageHandler(HttpMessageInvoker httpMessageInvoker, IHub hub)
        {
            _httpMessageInvoker = httpMessageInvoker;
            _hub = hub;
        }

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler(HttpMessageHandler innerHandler, IHub hub)
            : this(new HttpMessageInvoker(innerHandler, false), hub) {}

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler(HttpMessageHandler innerHandler)
            : this(innerHandler, HubAdapter.Instance) {}

        /// <summary>
        /// Initializes an instance of <see cref="SentryHttpMessageHandler"/>.
        /// </summary>
        public SentryHttpMessageHandler(IHub hub)
            : this(new HttpMessageInvoker(new HttpClientHandler(), true), hub) {}

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

            return _httpMessageInvoker.SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _httpMessageInvoker.Dispose();
            base.Dispose(disposing);
        }
    }
}
