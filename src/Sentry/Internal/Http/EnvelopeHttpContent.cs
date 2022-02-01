using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            _logger = logger;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            try
            {
                await _envelope.SerializeAsync(stream, _logger).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger?.LogError("Failed to serialize Envelope into the network stream", e);
                throw;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
