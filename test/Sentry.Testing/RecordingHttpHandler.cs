using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal.Extensions;

namespace Sentry.Testing
{
    public class RecordingHttpHandler : DelegatingHandler
    {
        private readonly List<HttpRequestMessage> _requests = new();
        private readonly Func<HttpResponseMessage> _getFakeResponse;

        public RecordingHttpHandler(Func<HttpResponseMessage> getFakeResponse = null)
        {
            _getFakeResponse = getFakeResponse;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requests.Add(request);

            if (_getFakeResponse is not null)
            {
                return _getFakeResponse();
            }

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
