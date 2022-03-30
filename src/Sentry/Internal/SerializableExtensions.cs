using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal static class SerializableExtensions
    {
        public static async Task<string> SerializeToStringAsync(
            this ISerializable serializable,
            IDiagnosticLogger logger,
            CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
#if NET461 || NETSTANDARD2_0
            using (stream)
#else
            await using (stream.ConfigureAwait(false))
#endif
            {
                await serializable.SerializeAsync(stream, logger, cancellationToken).ConfigureAwait(false);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static string SerializeToString(this ISerializable serializable, IDiagnosticLogger logger)
        {
            // TODO: Serialize synchronously
            return serializable.SerializeToStringAsync(logger).Result;
        }

        public static void Serialize(this ISerializable serializable, Stream stream, IDiagnosticLogger? logger)
        {
            // TODO: Serialize synchronously
            serializable.SerializeAsync(stream, logger).Wait();
        }
    }
}
