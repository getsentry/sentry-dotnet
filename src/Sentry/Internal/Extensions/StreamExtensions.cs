using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal.Extensions
{
    internal static class StreamExtensions
    {
        public static async Task<int> ReadByteAsync(
            this Stream stream,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new PooledBuffer<byte>(1);

            if (await stream.ReadAsync(buffer.Array, 0, 1, cancellationToken).ConfigureAwait(false) > 0)
            {
                return buffer.Array[0];
            }

            return -1;
        }

        public static async Task WriteByteAsync(
            this Stream stream,
            byte value,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new PooledBuffer<byte>(1);
            buffer.Array[0] = value;

            await stream.WriteAsync(buffer.Array, 0, 1, cancellationToken).ConfigureAwait(false);
        }
    }
}
