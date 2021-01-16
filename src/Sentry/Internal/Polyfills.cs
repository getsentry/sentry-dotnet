// ReSharper disable CheckNamespace
// ReSharper disable RedundantUsingDirective

// Polyfills to bridge the missing APIs in older versions of the framework/standard.
// In some cases, these just proxy calls to existing methods but also provide a signature that matches .netstd2.1

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;

#if NET461 || NETSTANDARD2_0
internal static partial class PolyfillExtensions
{
    public static string[] Split(this string str, char c, StringSplitOptions options = StringSplitOptions.None) =>
        str.Split(new[] {c}, options);

    public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken) =>
        stream.CopyToAsync(destination, 81920, cancellationToken);

    public static Task<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
        stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

    public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
        stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> pair,
        out TKey key,
        out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}

namespace System.Collections.Generic
{
    using Linq;

    internal static class PolyfillExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dic,
            TKey key,
            TValue defaultValue = default) =>
            dic.TryGetValue(key!, out var result) ? result! : defaultValue!;

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count) =>
            source.Reverse().Skip(count).Reverse();
    }
}
#endif

#if !NET5_0
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
}
#endif
