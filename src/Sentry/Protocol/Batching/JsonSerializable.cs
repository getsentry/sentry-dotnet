using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;

namespace Sentry.Protocol.Batching
{
    public class JsonSerializable : ISerializable
    {
        private readonly object _source;

        public JsonSerializable(object source) => _source = source;

        public async ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
            await Json.SerializeToStreamAsync(_source, stream, cancellationToken).ConfigureAwait(false);
    }
}
