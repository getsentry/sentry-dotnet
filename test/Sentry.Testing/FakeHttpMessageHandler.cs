using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _getResponse;
        private readonly List<HttpRequestMessage> _requests = new List<HttpRequestMessage>();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse)
        {
            _getResponse = getResponse;
        }

        public FakeHttpMessageHandler()
            : this(_ => new HttpResponseMessage(HttpStatusCode.OK))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return Task.FromResult(_getResponse(request));
        }

        public IReadOnlyList<HttpRequestMessage> GetRequests() => _requests.ToArray();
    }
}
