using System;
using System.IO.Compression;
using System.Net.Http;
using Sentry.Extensibility;
using Sentry.Http;

namespace Sentry.Internal.Http;

/// <summary>
/// Default Sentry HttpClientFactory
/// </summary>
/// <inheritdoc />
internal class DefaultSentryHttpClientFactory : ISentryHttpClientFactory
{
    /// <summary>
    /// Creates an <see cref="T:System.Net.Http.HttpClient" /> configure to call Sentry for the specified <see cref="T:Sentry.Dsn" />
    /// </summary>
    /// <param name="options">The HTTP options.</param>
    public HttpClient Create(SentryOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var httpClientHandler = options.CreateHttpClientHandler?.Invoke() ?? new HttpClientHandler();
        if (options.HttpProxy != null)
        {
            httpClientHandler.Proxy = options.HttpProxy;
            options.LogInfo("Using Proxy: {0}", options.HttpProxy);
        }

        // If the platform supports automatic decompression
        if (SupportsAutomaticDecompression(httpClientHandler))
        {
            // if the SDK is configured to accept compressed data
            httpClientHandler.AutomaticDecompression = options.DecompressionMethods;
        }
        else
        {
            options.LogDebug("No response compression supported by HttpClientHandler.");
        }

        HttpMessageHandler handler = httpClientHandler;

        if (options.RequestBodyCompressionLevel != CompressionLevel.NoCompression)
        {
            if (options.RequestBodyCompressionBuffered)
            {
                handler = new GzipBufferedRequestBodyHandler(handler, options.RequestBodyCompressionLevel);
                options.LogDebug("Using 'GzipBufferedRequestBodyHandler' body compression strategy with level {0}.", options.RequestBodyCompressionLevel);
            }
            else
            {
                handler = new GzipRequestBodyHandler(handler, options.RequestBodyCompressionLevel);
                options.LogDebug("Using 'GzipRequestBodyHandler' body compression strategy with level {0}.", options.RequestBodyCompressionLevel);
            }
        }
        else
        {
            options.LogDebug("Using no request body compression strategy.");
        }

        // Adding retry after last for it to run first in the pipeline
        handler = new RetryAfterHandler(handler);

        var client = new HttpClient(handler);

        client.DefaultRequestHeaders.Add("Accept", "application/json");

        if (options.ConfigureClient is { } configureClient)
        {
            options.LogDebug("Invoking user-defined HttpClient configuration action.");
            configureClient.Invoke(client);
        }

        return client;
    }

    private static bool SupportsAutomaticDecompression(HttpClientHandler handler)
    {
        // Workaround for https://github.com/getsentry/sentry-dotnet/issues/1561
        // OK to remove when fixed in .NET https://github.com/dotnet/runtime/issues/67529
        try
        {
            return handler.SupportsAutomaticDecompression;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }
}