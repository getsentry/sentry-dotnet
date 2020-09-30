using System;
using System.Net.Http;
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

        public IBackgroundWorker CreateBackgroundWorker()
        {
            if (_options.BackgroundWorker is { } worker)
            {
                _options.DiagnosticLogger?.LogDebug("Using IBackgroundWorker set through options: {0}.",
                    worker.GetType().Name);

                return worker;
            }

            if (_options.Dsn is null)
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

            var httpClient = _options.SentryHttpClientFactory?.Create(_options) ?? new HttpClient();

            return new BackgroundWorker(new HttpTransport(_options, httpClient, addAuth), _options);
        }
    }
}
