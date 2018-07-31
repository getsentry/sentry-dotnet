using System;
using System.IO.Compression;
using System.Net.Http;
using Sentry.Http;

namespace Sentry.Internal.Http
{
    /// <summary>
    /// Default Sentry HttpClientFactory
    /// </summary>
    /// <inheritdoc />
    internal class DefaultSentryHttpClientFactory : ISentryHttpClientFactory
    {
        private readonly Action<HttpClientHandler, Dsn, HttpOptions> _configureHandler;
        private readonly Action<HttpClient, Dsn, HttpOptions> _configureClient;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultSentryHttpClientFactory"/>
        /// </summary>
        /// <param name="configureHandler">An optional configuration callback</param>
        /// <param name="configureClient">An optional HttpClient configuration callback</param>
        public DefaultSentryHttpClientFactory(
            Action<HttpClientHandler, Dsn, HttpOptions> configureHandler = null,
            Action<HttpClient, Dsn, HttpOptions> configureClient = null)
        {
            _configureHandler = configureHandler;
            _configureClient = configureClient;
        }

        /// <summary>
        /// Creates an <see cref="T:System.Net.Http.HttpClient" /> configure to call Sentry for the specified <see cref="T:Sentry.Dsn" />
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="options">The HTTP options.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public HttpClient Create(Dsn dsn, HttpOptions options)
        {
            if (dsn == null)
            {
                throw new ArgumentNullException(nameof(dsn));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var httpClientHandler = new HttpClientHandler();
            if (options.Proxy != null)
            {
                httpClientHandler.Proxy = options.Proxy;
            }

            // If the platform supports automatic decompression
            if (httpClientHandler.SupportsAutomaticDecompression)
            {
                // if the SDK is configured to accept compressed data
                httpClientHandler.AutomaticDecompression = options.DecompressionMethods;
            }

            _configureHandler?.Invoke(httpClientHandler, dsn, options);

            HttpMessageHandler handler = httpClientHandler;

            if (options.RequestBodyCompressionLevel != CompressionLevel.NoCompression)
            {
                if (options.RequestBodyCompressionBuffered)
                {
                    handler = new GzipBufferedRequestBodyHandler(handler, options.RequestBodyCompressionLevel);
                }
                else
                {
                    handler = new GzipRequestBodyHandler(handler, options.RequestBodyCompressionLevel);
                }
            }

            // Adding retry after last for it to run first in the pipeline
            handler = new RetryAfterHandler(handler);

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Accept", "application/json");

            _configureClient?.Invoke(client, dsn, options);

            return client;
        }
    }
}
