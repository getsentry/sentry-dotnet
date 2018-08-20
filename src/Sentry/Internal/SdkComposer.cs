using System;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Internal.Http;

namespace Sentry.Internal
{
    internal class SdkComposer
    {
        private readonly SentryOptions _options;

        public SdkComposer(SentryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Dsn == null) throw new ArgumentException("No DSN defined in the SentryOptions");
        }

        public IBackgroundWorker CreateBackgroundWorker()
        {
            return _options.BackgroundWorker
                        ?? CreateBackgroundWorker(
                                CreateHttpTransport(
                                    _options.SentryHttpClientFactory
                                        ?? new DefaultSentryHttpClientFactory(
                                        _options.ConfigureHandler,
                                        _options.ConfigureClient),
                                        _options,
                                    _options),
                       _options);
        }

        private static BackgroundWorker CreateBackgroundWorker(
            ITransport transport,
            SentryOptions options)
            => new BackgroundWorker(transport, options);

        private static HttpTransport CreateHttpTransport(
            ISentryHttpClientFactory sentryHttpClientFactory,
            SentryOptions options,
            SentryOptions httpOptions)
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
