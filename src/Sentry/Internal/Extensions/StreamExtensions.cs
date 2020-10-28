using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal.Extensions
{
    internal static class StreamExtensions
    {
        public static async IAsyncEnumerable<byte> ReadAllBytesAsync(
            this Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var buffer = new PooledBuffer<byte>(1);

            while (await stream.ReadAsync(buffer.Array, 0, 1, cancellationToken).ConfigureAwait(false) > 0)
            {
                yield return buffer.Array[0];
            }
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
