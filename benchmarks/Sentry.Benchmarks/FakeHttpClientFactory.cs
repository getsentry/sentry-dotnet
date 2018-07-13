using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Http;

namespace Sentry.Benchmarks
{
    internal class FakeHttpClientFactory : ISentryHttpClientFactory
    {
        public HttpClient Create(Dsn dsn, HttpOptions options) => new HttpClient(new FakeMessageHandler());
    }

    internal class FakeMessageHandler : HttpMessageHandler
    {
        private readonly Task<HttpResponseMessage> _result
            = Task.FromResult<HttpResponseMessage>(new SentrySuccessResponse());

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _result;
    }

    internal class SentrySuccessResponse : HttpResponseMessage
    {
        public SentrySuccessResponse() : base(HttpStatusCode.OK) { }
    }

}
