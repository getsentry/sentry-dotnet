using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry.Internal.Http
{
    internal class SerializableHttpContent : HttpContent
    {
        private readonly ISerializable _envelope;

        public SerializableHttpContent(ISerializable envelope)
        {
            _envelope = envelope;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await _envelope.SerializeAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
