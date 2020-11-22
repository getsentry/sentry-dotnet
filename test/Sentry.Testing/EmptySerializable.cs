using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Testing
{
    public class EmptySerializable : ISerializable
    {
        public Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
