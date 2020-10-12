// ReSharper disable CheckNamespace

// Polyfills to bridge the missing APIs in older versions of the framework/standard.
// In some cases, these just proxy calls to existing methods but also provide a signature that matches .netstd2.1

#if NET461 || NETSTANDARD2_0
namespace System.IO
{
    using Threading;
    using Threading.Tasks;

    internal static class Extensions
    {
        public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken) =>
            stream.CopyToAsync(destination, 81920, cancellationToken);

        public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
    }
}
#endif
