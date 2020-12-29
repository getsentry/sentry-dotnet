using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal.Http
{
    /// <summary>
    /// Compresses the body of an HTTP request with GZIP.
    /// </summary>
    /// <inheritdoc />
    internal class GzipRequestBodyHandler : DelegatingHandler
    {
        private const string Gzip = "gzip";

        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Creates a new instance of <see cref="T:Sentry.Internal.Http.GzipRequestBodyHandler" />.
        /// </summary>
        /// <param name="innerHandler">The actual handler which handles the request.</param>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <exception cref="T:System.InvalidOperationException">Constructing this type with <see cref="T:System.IO.Compression.CompressionLevel" />
        /// of value <see cref="F:System.IO.Compression.CompressionLevel.NoCompression" /> is an invalid operation.</exception>
        /// <inheritdoc />
        public GzipRequestBodyHandler(HttpMessageHandler innerHandler, CompressionLevel compressionLevel)
            : base(innerHandler)
        {
            if (compressionLevel == CompressionLevel.NoCompression)
            {
                throw new InvalidOperationException($"Compression mode '{compressionLevel}' is invalid. Avoid registering the handler instead.");
            }

            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Sends the request while compressing its payload.
        /// </summary>
        /// <param name="request">The HTTP request to compress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                request.Content = new GzipContent(request.Content, _compressionLevel);
            }
            return base.SendAsync(request, cancellationToken);
        }

        // Internal for testability
        internal class GzipContent : HttpContent
        {
            private readonly HttpContent _content;
            private readonly CompressionLevel _compressionLevel;

            public GzipContent(HttpContent content, CompressionLevel compressionLevel)
            {
                _content = content;
                _compressionLevel = compressionLevel;

                foreach (var header in content.Headers)
                {
                    _ = Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                Headers.ContentEncoding.Add(Gzip);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                var gzipStream = new GZipStream(stream, _compressionLevel, leaveOpen: true);
                try
                {
                    await _content.CopyToAsync(gzipStream).ConfigureAwait(false);
                }
                finally
                {
                    gzipStream.Dispose();
                }
            }
        }
    }
}
