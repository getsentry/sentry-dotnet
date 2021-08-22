using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal static class SerializableExtensions
    {
        public static async Task<string> SerializeToStringAsync(
            this ISerializable serializable,
            CancellationToken cancellationToken = default)
        {
#if !NET461 && !NETSTANDARD2_0
            await
#endif
                using var stream = new MemoryStream();
            await serializable.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
