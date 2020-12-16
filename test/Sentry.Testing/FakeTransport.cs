using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;

namespace Sentry.Testing
{
    internal class FakeTransport : ITransport, IDisposable
    {
        private readonly List<Envelope> _envelopes = new();

        public event EventHandler<Envelope> EnvelopeSent;

        public Task SendEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _envelopes.Add(envelope);
            EnvelopeSent?.Invoke(this, envelope);

            return Task.CompletedTask;
        }

        public IReadOnlyList<Envelope> GetSentEnvelopes() => _envelopes.ToArray();

        public void Dispose() => _envelopes.DisposeAll();
    }
}
