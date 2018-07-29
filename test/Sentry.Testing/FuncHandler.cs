using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public class FuncHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> SendAsyncFunc { get; set; }

        public bool SendAsyncCalled { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendAsyncCalled = true;
            return Task.FromResult(SendAsyncFunc?.Invoke(request, cancellationToken));
        }
    }
}
