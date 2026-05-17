namespace Sentry.Internal;

internal static class ActivityExtensions
{
    public static SpanId AsSentrySpanId(this ActivitySpanId id) => SpanId.Parse(id.ToHexString());

    public static ActivitySpanId AsActivitySpanId(this SpanId id)
    {
#if NET8_0_OR_GREATER
        Span<byte> buffer = stackalloc byte[8];
        id.TryWriteBytes(buffer);
        return ActivitySpanId.CreateFromBytes(buffer);
#else
        Span<char> buffer = stackalloc char[16];
        id.TryFormat(buffer);
        return ActivitySpanId.CreateFromString(buffer);
#endif
    }

    public static SentryId AsSentryId(this ActivityTraceId id) => SentryId.Parse(id.ToHexString());

    public static ActivityTraceId AsActivityTraceId(this SentryId id)
    {
#if NET8_0_OR_GREATER
        Span<byte> buffer = stackalloc byte[16];
        id.TryWriteBytes(buffer);
        return ActivityTraceId.CreateFromBytes(buffer);
#else
        Span<char> buffer = stackalloc char[32];
        id.TryFormat(buffer);
        return ActivityTraceId.CreateFromString(buffer);
#endif
    }
}
