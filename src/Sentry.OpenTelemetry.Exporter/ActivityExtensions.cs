namespace Sentry.Internal;

internal static class ActivityExtensions
{
    private const int HexCharsPerByte = 2;
    private const int SpanIdByteCount = sizeof(long);
    private const int SpanIdHexCharCount = SpanIdByteCount * HexCharsPerByte;
    private static readonly int TraceIdByteCount = Unsafe.SizeOf<Guid>();
    internal static readonly int TraceIdHexCharCount = TraceIdByteCount * HexCharsPerByte;

    public static SpanId AsSentrySpanId(this ActivitySpanId id) => SpanId.Parse(id.ToHexString());

    public static ActivitySpanId AsActivitySpanId(this SpanId id)
    {
#if NET8_0_OR_GREATER
        Span<byte> buffer = stackalloc byte[SpanIdByteCount];
        id.TryWriteBytes(buffer);
        return ActivitySpanId.CreateFromBytes(buffer);
#else
        Span<char> buffer = stackalloc char[SpanIdHexCharCount];
        id.TryFormat(buffer);
        return ActivitySpanId.CreateFromString(buffer);
#endif
    }

    public static SentryId AsSentryId(this ActivityTraceId id) => SentryId.Parse(id.ToHexString());

    public static ActivityTraceId AsActivityTraceId(this SentryId id)
    {
#if NET8_0_OR_GREATER
        Span<byte> buffer = stackalloc byte[TraceIdByteCount];
        id.TryWriteBytes(buffer);
        return ActivityTraceId.CreateFromBytes(buffer);
#else
        Span<char> buffer = stackalloc char[TraceIdHexCharCount];
        id.TryFormat(buffer);
        return ActivityTraceId.CreateFromString(buffer);
#endif
    }
}
