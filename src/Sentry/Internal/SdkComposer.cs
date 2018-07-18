using System;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Internal.Http;

namespace Sentry.Internal
{
    internal class SdkComposer
    {
        private readonly SentryOptions _options;
        private readonly HttpOptions _httpOptions;
        private readonly BackgroundWorkerOptions _workerOptions;

        public SdkComposer(SentryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Dsn == null) throw new ArgumentException("No DSN defined in the SentryOptions");

            var httpOptions = new HttpOptions(options.Dsn.SentryUri);
            // TODO: ensure correct order
            options.ConfigureHttpTransportOptions?.ForEach(o => o.Invoke(httpOptions));
            _httpOptions = httpOptions;

            var workerOptions = new BackgroundWorkerOptions();
            options.ConfigureBackgroundWorkerOptions?.Invoke(workerOptions);
            _workerOptions = workerOptions;
        }

        public IBackgroundWorker CreateBackgroundWorker()
        {
            return _workerOptions.BackgroundWorker
                        ?? CreateBackgroundWorker(
                                CreateHttpTransport(
                                    _httpOptions.SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory(),
                                        _options,
                                        _httpOptions),
                                    _workerOptions);
        }

        private static BackgroundWorker CreateBackgroundWorker(
            ITransport transport,
            BackgroundWorkerOptions options)
            => new BackgroundWorker(transport, options);

        private static HttpTransport CreateHttpTransport(
            ISentryHttpClientFactory sentryHttpClientFactory,
            SentryOptions options,
            HttpOptions httpOptions)
        {
            var addAuth = SentryHeaders.AddSentryAuth(
               options.SentryVersion,
               options.ClientVersion,
               options.Dsn.PublicKey,
               options.Dsn.SecretKey);

            var httpClient = sentryHttpClientFactory.Create(options.Dsn, httpOptions);

            return new HttpTransport(
                httpOptions,
                httpClient,
                addAuth);
        }
    }
}
