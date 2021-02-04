using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _getResponse;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse) =>
            _getResponse = getResponse;

        public FakeHttpMessageHandler(Func<HttpResponseMessage> getResponse)
            : this(_ => getResponse()) {}

        public FakeHttpMessageHandler() {}

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _getResponse is not null
                    ? _getResponse(request)
                    : new HttpResponseMessage(HttpStatusCode.OK)
            );
        }
    }
}
