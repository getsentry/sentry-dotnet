using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class InMemoryBackgroundWorker : BackgroundWorkerBase
    {
        private readonly List<Envelope> _queue = new List<Envelope>();

        public InMemoryBackgroundWorker(ITransport transport, SentryOptions options)
            : base(transport, options)
        {
        }

        protected override ValueTask<Envelope?> TryGetNextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<Envelope?>(_queue.FirstOrDefault());
        }

        protected override int GetQueueLength() => _queue.Count;

        protected override void AddToQueue(Envelope envelope) => _queue.Add(envelope);

        protected override void RemoveFromQueue(Envelope envelope) => _queue.Remove(envelope);
    }
}
