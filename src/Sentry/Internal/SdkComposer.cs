using System;
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
            if (_options.SentryHttpClientFactory is { })
            {
                _options.DiagnosticLogger?.LogDebug("Using ISentryHttpClientFactory set through options: {0}.",
                    _options.SentryHttpClientFactory.GetType().Name);
            }

            var httpClientFactory = _options.SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory();
            var httpClient = httpClientFactory.Create(_options);

            var httpTransport = new HttpTransport(_options, httpClient);

            if (string.IsNullOrWhiteSpace(_options.CacheDirectoryPath))
            {
                return httpTransport;
            }

            return new CachingTransport(httpTransport, _options);
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
