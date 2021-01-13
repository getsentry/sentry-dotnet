using System;
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

        public static async Task<byte[]> ReadByteChunkAsync(
            this Stream stream,
            int expectedLength,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new PooledBuffer<byte>(expectedLength);
            var bytesRead = await stream.ReadAsync(buffer.Array, 0, expectedLength, cancellationToken)
                .ConfigureAwait(false);

            // The buffer is rented so we can't return it, plus it may be larger than needed.
            // So we copy everything to a new buffer.
            var result = new byte[bytesRead];
            Array.Copy(buffer.Array, result, bytesRead);

            return result;
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

        public static long? TryGetLength(this Stream stream)
        {
            try
            {
                return stream.Length;
            }
            catch
            {
                return null;
            }
        }
    }
}
