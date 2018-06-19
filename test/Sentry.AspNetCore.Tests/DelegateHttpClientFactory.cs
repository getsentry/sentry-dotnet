using System;
using System.Net.Http;
using Sentry.Http;

namespace Sentry.AspNetCore.Tests
{
    internal class DelegateHttpClientFactory : ISentryHttpClientFactory
    {
        private readonly Func<Dsn, HttpOptions, HttpClient> _clientFactory;

        public DelegateHttpClientFactory(Func<Dsn, HttpOptions, HttpClient> clientFactory)
            => _clientFactory = clientFactory;

        public HttpClient Create(Dsn dsn, HttpOptions options) => _clientFactory(dsn, options);
    }
}
