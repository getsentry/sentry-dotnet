// Polyfills to bridge the missing APIs in older targets.

#if !NET9_0_OR_GREATER
global using Lock = object;
#endif

#if NETFRAMEWORK || NETSTANDARD2_0
namespace System
{
    internal static class HashCode
    {
        public static int Combine<T1, T2>(T1 value1, T2 value2) => (value1, value2).GetHashCode();
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3) => (value1, value2, value3).GetHashCode();
    }
}

internal static partial class PolyfillExtensions
{
    public static StringBuilder AppendJoin(this StringBuilder builder, char separator, params object?[] values)
    {
        if (values.Length == 0)
        {
            return builder;
        }

        if (values[0] is {} value)
        {
            builder.Append(value);
        }

        for (var i = 1; i < values.Length; i++)
        {
            builder.Append(separator);
            if (values[i] is {} nextValue)
            {
                builder.Append(nextValue);
            }
        }
        return builder;
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
#endif

namespace System.Net.Http
{
    internal abstract class SerializableHttpContent : HttpContent
    {
#if !NET5_0_OR_GREATER
        protected virtual void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
        }

        internal Stream ReadAsStream(CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            SerializeToStream(stream, null, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
#endif
    }
}

#if !NET5_0_OR_GREATER
internal static partial class PolyfillExtensions
{
    public static Stream ReadAsStream(this HttpContent content, CancellationToken cancellationToken = default) =>
        content is SerializableHttpContent serializableContent
            ? serializableContent.ReadAsStream(cancellationToken)
            : content.ReadAsStreamAsync(cancellationToken).Result;
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
