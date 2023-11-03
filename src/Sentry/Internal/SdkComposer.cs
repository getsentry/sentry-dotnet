using Sentry.Extensibility;
using Sentry.Internal.Http;

namespace Sentry.Internal;

internal class SdkComposer
{
    private readonly SentryOptions _options;

    public SdkComposer(SentryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (options.Dsn is null)
        {
            throw new ArgumentException("No DSN defined in the SentryOptions");
        }
    }

    private ITransport CreateTransport()
    {
        // Start from either the transport given on options, or create a new HTTP transport.
        var transport = _options.Transport ?? CreateHttpTransport();

        // When a cache directory path is given, wrap the transport in a caching transport.
        if (!string.IsNullOrWhiteSpace(_options.CacheDirectoryPath))
        {
            transport = CachingTransport.Create(transport, _options);
        }

        // Always persist the transport on the options, so other places can pick it up where necessary.
        _options.Transport = transport;

        return transport;
    }

    private LazyHttpTransport CreateHttpTransport()
    {
        if (_options.SentryHttpClientFactory is { })
        {
            _options.LogDebug(
                "Using ISentryHttpClientFactory set through options: {0}.",
                _options.SentryHttpClientFactory.GetType().Name);
        }

        return new LazyHttpTransport(_options);
    }

    public IBackgroundWorker CreateBackgroundWorker()
    {
        if (_options.BackgroundWorker is { } worker)
        {
            _options.LogDebug("Using IBackgroundWorker set through options: {0}.",
                worker.GetType().Name);

            return worker;
        }

        var transport = CreateTransport();

        return new BackgroundWorker(transport, _options);
    }
}
