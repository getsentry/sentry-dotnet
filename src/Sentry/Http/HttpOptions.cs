using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace Sentry.Http
{
    /// <summary>
    /// HTTP options
    /// </summary>
    public class HttpOptions
    {
        internal Uri SentryUri { get; }

        /// <summary>
        /// Use 'Accept-Encoding: gzip'
        /// </summary>
        public bool AcceptGzip { get; set; } = true;

        /// <summary>
        /// Use 'Accept-Encoding: deflate'
        /// </summary>
        public bool AcceptDeflate { get; set; } = true;

        /// <summary>
        /// An optional web proxy
        /// </summary>
        public IWebProxy Proxy { get; set; }

        public ISentryHttpClientFactory SentryHttpClientFactory { get; set; }

        // Expected to call into the internal logging which will be expose
        internal Action<SentryEvent, HttpStatusCode, string> HandleFailedEventSubmission { get; set; }

        internal Func<HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        public HttpOptions(Uri sentryUri)
        {
            SentryUri = sentryUri;

#if DEBUG // Leave it null by default otherwise
            HandleFailedEventSubmission = (evt, status, msg) =>
            {
                Debug.WriteLine($"Sentry responded with status '{status}' for event {evt.EventId}. Message: {msg}");
            };
#endif
        }
    }
}
