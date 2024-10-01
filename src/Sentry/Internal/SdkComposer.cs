using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Infrastructure;
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
        _options.LogDebug("Creating transport.");

        // Start from either the transport given on options, or create a new HTTP transport.
        var transport = _options.Transport ?? CreateHttpTransport();

        // When a cache directory path is given, wrap the transport in a caching transport.
        if (!string.IsNullOrWhiteSpace(_options.CacheDirectoryPath))
        {
            _options.LogDebug("Cache directory path is specified.");

            if (_options.DisableFileWrite)
            {
                _options.LogInfo("File writing is disabled, Skipping caching transport creation.");
            }
            else
            {
                _options.LogDebug("File writing is enabled, wrapping transport in caching transport.");
                transport = CachingTransport.Create(transport, _options);
            }
        }
        else
        {
            _options.LogDebug("No cache directory path specified. Skipping caching transport creation.");
        }

        // Wrap the transport with the Spotlight one that double sends the envelope: Sentry + Spotlight
        if (_options.EnableSpotlight)
        {
            var environment = _options.SettingLocator.GetEnvironment(true);
            if (string.Equals(environment, Constants.ProductionEnvironmentSetting, StringComparison.OrdinalIgnoreCase))
            {
                _options.LogWarning("""
                                    [Spotlight] It seems you're not in dev mode because environment is set to 'production'.
                                    Do you really want to have Spotlight enabled?
                                    You can set a different environment via SENTRY_ENVIRONMENT env var or programatically during Init.
                                    Docs on Environment: https://docs.sentry.io/platforms/dotnet/configuration/environments/
                                    """);
            }
            else
            {
                _options.LogInfo("Connecting to Spotlight at {0}", _options.SpotlightUrl);
            }
            if (!Uri.TryCreate(_options.SpotlightUrl, UriKind.Absolute, out var spotlightUrl))
            {
                throw new InvalidOperationException("Invalid option for SpotlightUrl: " + _options.SpotlightUrl);
            }
            transport = new SpotlightHttpTransport(transport, _options, _options.GetHttpClient(), spotlightUrl, SystemClock.Clock);
        }

        // Always persist the transport on the options, so other places can pick it up where necessary.
        _options.Transport = transport;

        return transport;
    }

    private LazyHttpTransport CreateHttpTransport()
    {
        if (_options.SentryHttpClientFactory is not null)
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
