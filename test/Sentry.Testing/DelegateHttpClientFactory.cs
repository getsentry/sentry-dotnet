using System;
using System.Net.Http;
using Sentry.Http;

namespace Sentry.Testing
{
    public class DelegateHttpClientFactory : ISentryHttpClientFactory
    {
        private readonly Func<SentryOptions, HttpClient> _clientFactory;

        public DelegateHttpClientFactory(Func<SentryOptions, HttpClient> clientFactory)
            => _clientFactory = clientFactory;

        public HttpClient Create(SentryOptions options) => _clientFactory(options);
    }
}
