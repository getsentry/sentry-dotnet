using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal.Http
{
    /// <summary>
    /// Compresses the body of an HTTP request with GZIP while buffering the result.
    /// </summary>
    /// <remarks>
    /// This handler doesn't use 'Content-Encoding: chunked' as it sets the 'Content-Length' of the request.
    /// </remarks>
    /// <inheritdoc />
    internal class GzipBufferedRequestBodyHandler : DelegatingHandler
    {
        private const string Gzip = "gzip";

        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Creates a new instance of <see cref="T:Sentry.Internal.Http.GzipBufferedRequestBodyHandler" />.
        /// </summary>
        /// <param name="innerHandler">The actual handler which handles the request.</param>
        /// <param name="compressionLevel">The compression level to use.</param>
        /// <exception cref="T:System.InvalidOperationException">Constructing this type with <see cref="T:System.IO.Compression.CompressionLevel" />
        /// of value <see cref="F:System.IO.Compression.CompressionLevel.NoCompression" /> is an invalid operation.</exception>
        /// <inheritdoc />
        public GzipBufferedRequestBodyHandler(HttpMessageHandler innerHandler, CompressionLevel compressionLevel)
            : base(innerHandler)
        {
            if (compressionLevel == CompressionLevel.NoCompression)
            {
                throw new InvalidOperationException($"Compression mode '{compressionLevel}' is invalid. Avoid registering the handler instead.");
            }

            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Compresses the request body and sends a request with a buffered stream.
        /// </summary>
        /// <param name="request">The HTTP request to compress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();
            if (request.Content is not null)
            {
                using (var gzipStream = new GZipStream(memoryStream, _compressionLevel, leaveOpen: true))
                {
                    await request.Content.CopyToAsync(gzipStream).ConfigureAwait(false);
                }
                memoryStream.Position = 0;

                request.Content = new BufferedStreamContent(memoryStream, memoryStream.Length, request.Content.Headers);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Internal for testability
        internal class BufferedStreamContent : StreamContent
        {
            internal long ContentLength { get; }

            public BufferedStreamContent(Stream stream, long contentLength, HttpContentHeaders headers)
                : base(stream)
            {
                ContentLength = contentLength;

                foreach (var header in headers)
                {
                    _ = Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                Headers.ContentEncoding.Add(Gzip);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = ContentLength;
                return true;
            }
        }
    }
}
