namespace Sentry.Internal;

internal static class ActivityExtensions
{
    public static SpanId AsSentrySpanId(this ActivitySpanId id) => SpanId.Parse(id.ToHexString());

    public static ActivitySpanId AsActivitySpanId(this SpanId id) =>
        ActivitySpanId.CreateFromString(id.ToString().AsSpan());

    public static SentryId AsSentryId(this ActivityTraceId id) => SentryId.Parse(id.ToHexString());

    public static ActivityTraceId AsActivityTraceId(this SentryId id) =>
        ActivityTraceId.CreateFromString(id.ToString().AsSpan());
}
