using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public static class RandomExtensions
    {
        public static async Task WriteToStreamAsync(this Random random, Stream destination, long length, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[81920];
            var remainingBytes = length;

            while (remainingBytes > 0)
            {
                random.NextBytes(buffer);
                var bytesToWrite = (int)Math.Min(remainingBytes, buffer.Length);

                await destination.WriteAsync(buffer, 0, bytesToWrite, cancellationToken);
                remainingBytes -= bytesToWrite;
            }
        }
    }
}
