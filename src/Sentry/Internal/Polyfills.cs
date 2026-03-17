// Polyfills to bridge the missing APIs in older targets.

#if NETFRAMEWORK || NETSTANDARD2_0
namespace System
{
    internal static class HashCode
    {
        public static int Combine<T1, T2>(T1 value1, T2 value2) => (value1, value2).GetHashCode();
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3) => (value1, value2, value3).GetHashCode();
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

// TODO: remove when updating Polyfill: https://github.com/getsentry/sentry-dotnet/pull/4879
#if !NET6_0_OR_GREATER
internal static class EnumerableExtensions
{
    internal static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is ICollection<TSource> genericCollection)
        {
            count = genericCollection.Count;
            return true;
        }

        if (source is ICollection collection)
        {
            count = collection.Count;
            return true;
        }

        count = 0;
        return false;
    }
}
#endif

// TODO: remove when updating Polyfill: https://github.com/getsentry/sentry-dotnet/pull/4879
#if !NET6_0_OR_GREATER
internal static class ArgumentNullExceptionExtensions
{
    extension(ArgumentNullException)
    {
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                Throw(paramName);
            }
        }
    }

    [DoesNotReturn]
    private static void Throw(string? paramName)
    {
        throw new ArgumentNullException(paramName);
    }
}
#endif
