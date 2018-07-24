using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace Sentry.Http
{
    /// <summary>
    /// HTTP options
    /// </summary>
    public class HttpOptions
    {
        internal Uri SentryUri { get; }

        /// <summary>
        /// Decompression methods accepted
        /// </summary>
        /// <remarks>
        /// By default accepts all available compression methods supported by the platform
        /// </remarks>
        public DecompressionMethods DecompressionMethods { get; set; }
            // Note the ~ enabling all bits
            = ~DecompressionMethods.None;

        /// <summary>
        /// The level of which to compress the <see cref="SentryEvent"/> before sending to Sentry
        /// </summary>
        /// <remarks>
        /// To disable request body compression, use <see cref="CompressionLevel.NoCompression"/>
        /// </remarks>
        public CompressionLevel RequestBodyCompressionLevel { get; set; } = CompressionLevel.Optimal;

        /// <summary>
        /// An optional web proxy
        /// </summary>
        public IWebProxy Proxy { get; set; }

        public ISentryHttpClientFactory SentryHttpClientFactory { get; set; }

        // Expected to call into the internal logging which will be expose
        internal Action<SentryEvent, HttpStatusCode, string> HandleFailedEventSubmission { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOptions"/> class.
        /// </summary>
        /// <param name="sentryUri">The sentry URI.</param>
        public HttpOptions(Uri sentryUri)
        {
            if (sentryUri == null)
            {
                throw new ArgumentNullException(nameof(sentryUri));
            }

            if (!sentryUri.IsAbsoluteUri)
            {
                throw new ArgumentException(
                    "URL to Sentry must be absolute. Example: https://98718479@sentry.io/123456", nameof(sentryUri));
            }

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
