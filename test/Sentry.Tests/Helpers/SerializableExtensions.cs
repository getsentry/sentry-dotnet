using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Tests.Helpers
{
    internal static class SerializableExtensions
    {
        public static async Task<string> SerializeToStringAsync(
            this ISerializable serializable,
            CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await serializable.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
