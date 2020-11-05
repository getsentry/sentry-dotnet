using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class StatelessBackgroundWorker : BackgroundWorkerBase
    {
        public StatelessBackgroundWorker(ITransport transport, SentryOptions options)
            : base(transport, options)
        {
        }

        protected override async ValueTask ProcessEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            await Transport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}
