using System;
using System.Net.Http;
using Sentry.Http;

namespace Sentry.Testing
{
    public class DelegateHttpClientFactory : ISentryHttpClientFactory
    {
        private readonly Func<Dsn, SentryOptions, HttpClient> _clientFactory;

        public DelegateHttpClientFactory(Func<Dsn, SentryOptions, HttpClient> clientFactory)
            => _clientFactory = clientFactory;

        public HttpClient Create(Dsn dsn, SentryOptions options) => _clientFactory(dsn, options);
    }
}
