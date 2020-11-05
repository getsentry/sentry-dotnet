using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class CachingBackgroundWorker : BackgroundWorkerBase
    {
        public CachingBackgroundWorker(ITransport transport, SentryOptions options)
            : base(transport, options)
        {
        }

        protected override ValueTask ProcessEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
