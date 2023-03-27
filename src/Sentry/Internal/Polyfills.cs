// Polyfills to bridge the missing APIs in older versions of the framework/standard.
// In some cases, these just proxy calls to existing methods but also provide a signature that matches .netstd2.1

#if !NET5_0_OR_GREATER
using Sentry.Internal.Http;
#endif

#if NETFRAMEWORK || NETSTANDARD2_0
internal static partial class PolyfillExtensions
{
    public static Task<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
        stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

    public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
        stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
}

namespace System.Collections.Generic
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class PolyfillExtensions
    {
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count) =>
            source.Reverse().Skip(count).Reverse();
    }
}

namespace System
{
    internal static class HashCode
    {
        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                var hashCode = value1 != null ? value1.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (value2 != null ? value2.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
        {
            unchecked
            {
                var hashCode = value1 != null ? value1.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (value2 != null ? value2.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (value3 != null ? value3.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}

#endif

#if NETFRAMEWORK
namespace System.Net.Http.Headers
{
    // This class just helps resolve the namespace for the global using.
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class Shim
    {
    }
}

namespace System.Linq
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class PolyfillExtensions
    {
        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            foreach (var item in source)
            {
                yield return item;
            }

            yield return element;
        }
    }
}
#endif

#if !NET5_0_OR_GREATER
internal static partial class PolyfillExtensions
{
    public static Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken cancellationToken = default) =>
        !cancellationToken.IsCancellationRequested
            ? content.ReadAsStringAsync()
            : Task.FromCanceled<string>(cancellationToken);

    public static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken cancellationToken = default) =>
        !cancellationToken.IsCancellationRequested
            ? content.ReadAsStreamAsync()
            : Task.FromCanceled<Stream>(cancellationToken);

    public static Stream ReadAsStream(this HttpContent content)
    {
        if (content is EnvelopeHttpContent envelopeHttpContent)
        {
            var stream = new MemoryStream();
            envelopeHttpContent.SerializeToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        return content.ReadAsStreamAsync().Result;
    }
}
#endif

#if !NET6_0_OR_GREATER
internal static partial class PolyfillExtensions
{
    public static void WriteRawValue(this Utf8JsonWriter writer, byte[] utf8Json)
    {
        using var document = JsonDocument.Parse(utf8Json);
        document.RootElement.WriteTo(writer);
    }
}
#endif
