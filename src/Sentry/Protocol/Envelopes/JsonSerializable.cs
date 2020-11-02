using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;

namespace Sentry.Protocol.Envelopes
{
    public class JsonSerializable : ISerializable
    {
        public object Source { get; }

        public JsonSerializable(object source) => Source = source;

        public async ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
            await Json.SerializeToStreamAsync(Source, stream, cancellationToken).ConfigureAwait(false);
    }
}
