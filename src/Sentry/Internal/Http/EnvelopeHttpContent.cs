using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http
{
    internal class EnvelopeHttpContent : HttpContent
    {
        private readonly Envelope _envelope;

        public EnvelopeHttpContent(Envelope envelope) => _envelope = envelope;

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            await _envelope.SerializeAsync(stream).ConfigureAwait(false);

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
