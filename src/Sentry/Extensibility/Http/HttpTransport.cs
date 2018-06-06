using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Extensibility.Http
{
    public class HttpTransport : ITransport
    {
        public Task CaptureEventAsync(SentryEvent @event, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
