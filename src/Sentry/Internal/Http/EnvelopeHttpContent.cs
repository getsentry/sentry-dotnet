using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

#if NET5_0_OR_GREATER
using System.Threading;
#endif

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

#if NET5_0_OR_GREATER
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
#else
        internal void SerializeToStream(Stream stream)
#endif
        {
            try
            {
                _envelope.Serialize(stream, _logger);
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
