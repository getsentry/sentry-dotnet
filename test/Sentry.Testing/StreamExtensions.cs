using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public static class StreamExtensions
    {
        private static readonly Random _random = new();

        public static async Task FillWithRandomBytesAsync(this Stream stream, long length)
        {
            var remainingLength = length;
            var buffer = new byte[81920];

            while (remainingLength > 0)
            {
                _random.NextBytes(buffer);

                var bytesToCopy = (int) Math.Min(remainingLength, buffer.Length);
                await stream.WriteAsync(buffer, 0, bytesToCopy);

                remainingLength -= bytesToCopy;
            }
        }

        public static MemoryStream ToMemoryStream(this string source, Encoding encoding) =>
            new(encoding.GetBytes(source));

        public static MemoryStream ToMemoryStream(this string source) =>
            source.ToMemoryStream(Encoding.UTF8);
    }
}
