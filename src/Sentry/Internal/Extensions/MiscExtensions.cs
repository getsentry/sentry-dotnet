namespace Sentry.Internal.Extensions;

internal static class MiscExtensions
{
    public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> pipe) => pipe(input);

    public static T? NullIfDefault<T>(this T value) where T : struct =>
        !EqualityComparer<T>.Default.Equals(value, default)
            ? value
            : null;

    public static string ToHexString(this long l, bool upperCase = false) =>
        "0x" + l.ToString("x", CultureInfo.InvariantCulture);

    public static string ToHexString(this byte[] bytes, bool upperCase = false) =>
        new ReadOnlySpan<byte>(bytes).ToHexString(upperCase);

    public static string ToHexString(this Span<byte> bytes, bool upperCase = false) =>
        ((ReadOnlySpan<byte>)bytes).ToHexString(upperCase);

    public static string ToHexString(this ReadOnlySpan<byte> bytes, bool upperCase = false)
    {
#if NET5_0_OR_GREATER
        var s = Convert.ToHexString(bytes);
        return upperCase ? s : s.ToLowerInvariant();
#else
        var buffer = new StringBuilder(bytes.Length * 2);
        var format = upperCase ? "X2" : "x2";

        foreach (var t in bytes)
        {
            buffer.Append(t.ToString(format, CultureInfo.InvariantCulture));
        }

        return buffer.ToString();
#endif
    }

    private static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    public static void CancelAfterSafe(this CancellationTokenSource cts, TimeSpan timeout)
    {
        if (timeout == TimeSpan.Zero)
        {
            // CancelAfter(TimeSpan.Zero) may not cancel immediately, but Cancel always will.
            cts.Cancel();
        }
        else if (timeout > MaxTimeout)
        {
            // Timeout milliseconds can't be larger than int.MaxValue
            // Treat such values (i.e. TimeSpan.MaxValue) as an infinite timeout (-1 ms).
            cts.CancelAfter(Timeout.InfiniteTimeSpan);
        }
        else
        {
            // All other timeout values
            cts.CancelAfter(timeout);
        }
    }

    /// <summary>
    /// Determines whether an object is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// This method exists so that we can test for null in situations where a method might be called from
    /// code that ignores nullability warnings.
    /// (It prevents us having to have two different resharper ignore comments depending on target framework.)
    /// </remarks>
    public static bool IsNull(this object? o) => o is null;

    public static void Add<TKey, TValue>(
        this ICollection<KeyValuePair<TKey, TValue>> collection,
        TKey key,
        TValue value) =>
        collection.Add(new KeyValuePair<TKey, TValue>(key, value));

    internal static string GetRawMessage(this AggregateException exception)
    {
        var message = exception.Message;
        if (exception.InnerException is { } inner)
        {
            var i = message.IndexOf($" ({inner.Message})", StringComparison.Ordinal);
            if (i > 0)
            {
                return message[..i];
            }
        }

        return message;
    }
}
