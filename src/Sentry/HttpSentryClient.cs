using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry
{
    ///
    public class HttpSentryClient : ISentryClient, IDisposable
    {
        ///
        public HttpSentryClient(SentryOptions options = null)
        {
        }

        ///
        public Task<SentryResponse> CaptureEventAsync(SentryEvent @event, Scope scope, CancellationToken cancellationToken = default)
            => Task.FromResult(new SentryResponse(false));

        ///
        public SentryResponse CaptureEvent(SentryEvent @event, Scope scope) => new SentryResponse(false);

        ///
        public void Dispose()
        {
        }
    }
}
