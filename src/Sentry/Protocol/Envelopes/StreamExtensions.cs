#if WINDOWS_UWP
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol.Envelopes
{
    internal static class StreamExtensions
    {
        internal static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        internal static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken)
        {
            return stream.CopyToAsync(destination, GetStreamSize(stream), cancellationToken);
        }

        private static int  GetStreamSize(Stream stream)
        {
            var currentPosition = stream.Position;
            _ = stream.Seek(0, SeekOrigin.End);
            var length = stream.Position;
            _ = stream.Seek(currentPosition, SeekOrigin.Begin);
            return (int)length;
        }
    }
}
#endif
