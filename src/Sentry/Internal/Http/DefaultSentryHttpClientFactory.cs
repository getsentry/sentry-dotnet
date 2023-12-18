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

        var handler = options.CreateHttpMessageHandler?.Invoke() ?? new HttpClientHandler();
        if (handler is HttpClientHandler httpClientHandler)
        {
            if (options.HttpProxy is not null)
            {
                // [CA1416] This call site is reachable on: 'ios' 10.0 and later, 'maccatalyst' 10.0 and later. 'HttpClientHandler.Proxy' is unsupported on: 'ios' all versions, 'maccatalyst' all versions.
#if NET6_0_OR_GREATER
                if (!OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst())
#endif
                {
                    httpClientHandler.Proxy = options.HttpProxy;
                    options.LogInfo("Using Proxy: {0}", options.HttpProxy);
                }
            }

            // If the platform supports automatic decompression
            if (SupportsAutomaticDecompression(httpClientHandler))
            {
                // if the SDK is configured to accept compressed data
                httpClientHandler.AutomaticDecompression = options.DecompressionMethods;
            }
            else
            {
                options.LogWarning("No response compression supported by HttpClientHandler.");
            }
        }
#if IOS
        if (handler is System.Net.Http.NSUrlSessionHandler nsUrlSessionHandler)
        {
            if (options.HttpProxy != null)
            {
                bool supportsProxy = false;
                if (nsUrlSessionHandler.SupportsProxy)
                {
                    supportsProxy = true;
                    try
                    {
                        // Code analysis reports this as error, since it is marked as unsupported.
                        // Being aware of that this code is meant to support this feature as soon as
                        // <see cref="T:System.Net.Http.NSUrlSessionHandler" /> supports it.
#pragma warning disable CA1416
                        nsUrlSessionHandler.Proxy = options.HttpProxy;
#pragma warning restore CA1416
                        options.LogInfo("Using Proxy: {0}", options.HttpProxy);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        supportsProxy = false;
                    }
                }
                if (!supportsProxy)
                {
                    options.LogWarning("No proxy supported by NSUrlSessionHandler.");
                }
            }

            // If the platform supports automatic decompression
            bool compressionSupported = false;
            try
            {
                if (nsUrlSessionHandler.SupportsAutomaticDecompression)
                {
                    // if the SDK is configured to accept compressed data
                    nsUrlSessionHandler.AutomaticDecompression = options.DecompressionMethods;
                    compressionSupported = true;
                }
            }
            catch (PlatformNotSupportedException)
            {
                compressionSupported = false;
            }
            if (!compressionSupported)
            {
                options.LogInfo("No response compression supported by NSUrlSessionHandler.");
            }
        }
#endif

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
