using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Extensibility.Http
{
    public class HttpTransport : ITransport
    {
        public Task<SentryResponse> CaptureEventAsync(SentryEvent @event, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public SentryResponse CaptureEvent(SentryEvent @event)
        {
            throw new NotImplementedException();
        }
    }
}
