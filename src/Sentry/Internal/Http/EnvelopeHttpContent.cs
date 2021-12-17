using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http
{
    internal class EnvelopeHttpContent : HttpContent
    {
        private readonly Envelope _envelope;
        private readonly IDiagnosticLogger? _logger;

        public EnvelopeHttpContent(Envelope envelope, IDiagnosticLogger? logger)
        {
            _envelope = envelope;
            _logger = logger;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            await _envelope.SerializeAsync(stream, _logger).ConfigureAwait(false);

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
