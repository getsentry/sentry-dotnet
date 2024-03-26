namespace Sentry.Internal.Tracing;

internal static class TraceExtensions
{
    public static SpanId AsSentrySpanId(this ActivitySpanId id) => SpanId.Parse(id.ToHexString());

    public static ActivitySpanId AsActivitySpanId(this SpanId id) => ActivitySpanId.CreateFromString(id.ToString().AsSpan());

    public static SentryId AsSentryId(this ActivityTraceId id) => SentryId.Parse(id.ToHexString());

    public static ActivityTraceId AsActivityTraceId(this SentryId id) => ActivityTraceId.CreateFromString(id.ToString().AsSpan());

    public static BaggageHeader AsBaggageHeader(this IEnumerable<KeyValuePair<string, string?>> baggage, bool useSentryPrefix = false) =>
        BaggageHeader.Create(
            baggage.Where(member => member.Value != null)
                        .Select(kvp => (KeyValuePair<string, string>)kvp!),
            useSentryPrefix
            );
}
