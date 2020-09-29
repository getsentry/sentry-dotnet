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
            if (string.IsNullOrWhiteSpace(options.Dsn)) throw new ArgumentException("No DSN defined in the SentryOptions");
        }

        public IBackgroundWorker CreateBackgroundWorker()
        {
            if (_options.BackgroundWorker is { } worker)
            {
                _options.DiagnosticLogger?.LogDebug("Using IBackgroundWorker set through options: {0}.",
                    worker.GetType().Name);

                return worker;
            }

            if (string.IsNullOrWhiteSpace(_options.Dsn))
            {
                throw new InvalidOperationException("The DSN is expected to be set at this point.");
            }

            var dsn = Dsn.Parse(_options.Dsn);

            var addAuth = SentryHeaders.AddSentryAuth(
                _options.SentryVersion,
                _options.ClientVersion,
                dsn.PublicKey,
                dsn.SecretKey
            );

            if (_options.SentryHttpClientFactory is { } factory)
            {
                _options.DiagnosticLogger?.LogDebug("Using ISentryHttpClientFactory set through options: {0}.",
                    factory.GetType().Name);
            }
            else
            {
#pragma warning disable 618 // Tests will be removed once obsolete code gets removed
                factory = new DefaultSentryHttpClientFactory(_options.ConfigureHandler, _options.ConfigureClient);
#pragma warning restore 618
            }

            var httpClient = factory.Create(_options);

            return new BackgroundWorker(new HttpTransport(_options, httpClient, addAuth), _options);
        }
    }
}
