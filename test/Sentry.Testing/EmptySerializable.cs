using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry.Testing
{
    public class EmptySerializable : ISerializable
    {
        public ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default) => default;
    }
}
