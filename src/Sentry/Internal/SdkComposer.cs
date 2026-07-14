using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Infrastructure;
using Sentry.Internal.Http;

namespace Sentry.Internal;

internal class SdkComposer
{
    private readonly SentryOptions _options;
    private readonly BackpressureMonitor? _backpressureMonitor;

    public SdkComposer(SentryOptions options, BackpressureMonitor? backpressureMonitor)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        if (options.Dsn is null && !options.EnableSpotlight)
        {
            throw new ArgumentException("No DSN defined and Spotlight is disabled in the SentryOptions.");
        }
        _backpressureMonitor = backpressureMonitor;
    }

    private ITransport CreateTransport()
    {
        _options.LogDebug("Creating transport.");

        ITransport transport;
        var hasDsn = !string.IsNullOrWhiteSpace(_options.Dsn) && !Dsn.IsDisabled(_options.Dsn!);

        if (hasDsn)
        {
            // Start from either the transport given on options, or create a new HTTP transport.
            transport = _options.Transport ?? new LazyHttpTransport(_options, _backpressureMonitor);

            // When a cache directory path is given, wrap the transport in a caching transport.
            if (!string.IsNullOrWhiteSpace(_options.CacheDirectoryPath))
            {
                _options.LogDebug("Cache directory path is specified.");

                if (_options.DisableFileWrite)
                {
                    _options.LogInfo("File write has been disabled via the options. Skipping caching transport creation.");
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
        }
        else
        {
            // No DSN — use a no-op transport (e.g. Spotlight-only mode).
            _options.LogDebug("No DSN configured. Using no-op transport for Sentry.");
            transport = NoOpTransport.Instance;
        }

        // Create a separate Spotlight transport when enabled.
        // Unlike before, this is NOT a wrapper around the main transport — it sends independently.
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
            _options.SpotlightTransport = new SpotlightHttpTransport(_options, _options.GetHttpClient(), spotlightUrl, SystemClock.Clock);
        }

        // Always persist the transport on the options, so other places can pick it up where necessary.
        _options.Transport = transport;

        return transport;
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

        return new BackgroundWorker(transport, _options, _backpressureMonitor);
    }
}
