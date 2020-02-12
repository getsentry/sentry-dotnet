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
            if (_options.BackgroundWorker is IBackgroundWorker worker)
            {
                _options.DiagnosticLogger?.LogDebug("Using IBackgroundWorker set through options: {0}.",
                    worker.GetType().Name);

                return worker;
            }

            var addAuth = SentryHeaders.AddSentryAuth(
                _options.SentryVersion,
                _options.ClientVersion,
                _options.Dsn.PublicKey,
                _options.Dsn.SecretKey);

            if (_options.SentryHttpClientFactory is ISentryHttpClientFactory factory)
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

            var httpClient = factory.Create(_options.Dsn, _options);

            return new BackgroundWorker(new HttpTransport(_options, httpClient, addAuth), _options);
        }
    }
}
