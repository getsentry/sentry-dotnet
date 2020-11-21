using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public class FakeHttpClientHandler : HttpClientHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _getResponse;
        private readonly List<HttpRequestMessage> _requests = new List<HttpRequestMessage>();

        public FakeHttpClientHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> getResponse)
        {
            _getResponse = getResponse;
        }

        public FakeHttpClientHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse)
            : this(req => Task.FromResult(getResponse(req)))
        {
        }

        public FakeHttpClientHandler()
            : this(_ => new HttpResponseMessage(HttpStatusCode.OK))
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Store a cloned response, as the original will likely get disposed
            _requests.Add(await request.CloneAsync());
            return await _getResponse(request);
        }

        public IReadOnlyList<HttpRequestMessage> GetRequests() => _requests.ToArray();

        protected override void Dispose(bool disposing)
        {
            foreach (var request in _requests)
            {
                request.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
