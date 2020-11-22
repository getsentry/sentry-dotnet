using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Testing
{
    internal class FakeFailingTransport : ITransport
    {
        public Task SendEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            throw new Exception("Expected transport failure has occured.");
        }
    }
}
