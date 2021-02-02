using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal.Extensions;

namespace Sentry.Testing
{
    public class RecordingHttpMessageHandler : DelegatingHandler
    {
        private readonly List<HttpRequestMessage> _requests = new();

        public RecordingHttpMessageHandler() {}

        public RecordingHttpMessageHandler(HttpMessageHandler innerHandler) =>
            InnerHandler = innerHandler;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Clone the request to avoid ObjectDisposedException
            _requests.Add(await request.CloneAsync());

            InnerHandler ??= new HttpClientHandler();

            return await base.SendAsync(request, cancellationToken);
        }

        public IReadOnlyList<HttpRequestMessage> GetRequests() => _requests.ToArray();

        protected override void Dispose(bool disposing)
        {
            _requests.DisposeAll();
            base.Dispose(disposing);
        }
    }
}
