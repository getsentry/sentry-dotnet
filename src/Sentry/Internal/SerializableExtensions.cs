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

                stream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public static string SerializeToString(this ISerializable serializable, IDiagnosticLogger logger)
        {
            using var stream = new MemoryStream();
            serializable.Serialize(stream, logger);

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
