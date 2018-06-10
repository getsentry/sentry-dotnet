using System;
using System.Net;
using System.Net.Http;
using Sentry.Http;

namespace Sentry.Internal.Http
{
    /// <summary>
    /// Default Sentry HttpClientFactory
    /// </summary>
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
        /// Creates an <see cref="HttpClient"/> configure to call Sentry for the specified <see cref="Dsn"/>
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="options">The HTTP options.</param>
        /// <returns></returns>
        public HttpClient Create(Dsn dsn, HttpOptions options)
        {
            if (dsn == null)
            {
                throw new ArgumentNullException(nameof(dsn));
            }

            var httpClientHandler = CreateHttpClientHandler();

            // If the platform supports automatic decompression
            if (httpClientHandler.SupportsAutomaticDecompression)
            {
                // if the SDK is configured to accept GZip
                httpClientHandler.AutomaticDecompression = options.AcceptGzip
                    ? httpClientHandler.AutomaticDecompression | DecompressionMethods.GZip
                    : httpClientHandler.AutomaticDecompression & ~DecompressionMethods.GZip;

                // if the SDK is configured to accept Deflate
                httpClientHandler.AutomaticDecompression = options.AcceptDeflate
                    ? httpClientHandler.AutomaticDecompression | DecompressionMethods.Deflate
                      : httpClientHandler.AutomaticDecompression & ~DecompressionMethods.Deflate;
            }

            _configureHandler?.Invoke(httpClientHandler, dsn, options);

            var client = new HttpClient(httpClientHandler);

            client.DefaultRequestHeaders.Add("Accept", "application/json");

            _configureClient?.Invoke(client, dsn, options);

            return client;
        }

        /// <summary>
        /// Creates a new <see cref="HttpClientHandler"/>
        /// </summary>
        /// <returns><see cref="HttpClientHandler"/></returns>
        protected virtual HttpClientHandler CreateHttpClientHandler()
            // TODO: SocketsHttpHandler is only netcoreapp2.1?
            => new HttpClientHandler();
    }
}
