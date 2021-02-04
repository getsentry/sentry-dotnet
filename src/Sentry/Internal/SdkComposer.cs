using System;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Internal.Http;

namespace Sentry.Internal
{
    internal class SdkComposer
    {
        private readonly SentryOptions _options;

        public SdkComposer(SentryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Dsn is null) throw new ArgumentException("No DSN defined in the SentryOptions");
        }

        private ITransport CreateTransport()
        {
            // Override for tests
            if (_options.Transport is not null)
            {
                return _options.Transport;
            }

            if (_options.SentryHttpClientFactory is { })
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Using ISentryHttpClientFactory set through options: {0}.",
                    _options.SentryHttpClientFactory.GetType().Name
                );
            }

            var httpClientFactory = _options.SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory();
            var httpClient = httpClientFactory.Create(_options);

            var httpTransport = new HttpTransport(_options, httpClient);

            // Non-caching transport
            if (string.IsNullOrWhiteSpace(_options.CacheDirectoryPath))
            {
                return httpTransport;
            }

            // Caching transport
            var cachingTransport = new CachingTransport(httpTransport, _options);

            // If configured, flush existing cache
            if (_options.InitCacheFlushTimeout > TimeSpan.Zero)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Flushing existing cache during transport activation up to {0}.",
                    _options.InitCacheFlushTimeout
                );

                // Use a timeout to avoid waiting for too long
                using var timeout = new CancellationTokenSource(_options.InitCacheFlushTimeout);

                try
                {
                    cachingTransport.FlushAsync(timeout.Token).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    _options.DiagnosticLogger?.LogError(
                        "Flushing timed out."
                    );
                }
                catch (Exception ex)
                {
                    _options.DiagnosticLogger?.LogFatal(
                        "Flushing failed.",
                        ex
                    );
                }
            }

            return cachingTransport;
        }

        public IBackgroundWorker CreateBackgroundWorker()
        {
            if (_options.BackgroundWorker is { } worker)
            {
                _options.DiagnosticLogger?.LogDebug("Using IBackgroundWorker set through options: {0}.",
                    worker.GetType().Name);

                return worker;
            }

            var transport = CreateTransport();

            return new BackgroundWorker(transport, _options);
        }
    }
}
