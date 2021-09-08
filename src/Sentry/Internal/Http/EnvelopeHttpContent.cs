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

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            _envelope.SerializeAsync(stream);

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
