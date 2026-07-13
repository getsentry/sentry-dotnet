#if HAS_ACTIVITY_LISTENER
namespace Sentry.Internal.Tracing;

// Note: a copy of these conversions ships in Sentry.OpenTelemetry(.Exporter) as Sentry.Internal.ActivityExtensions.
// This copy lives in a distinct namespace to avoid CS0436 conflicts in the OTel packages, which can see Sentry's
// internals via InternalsVisibleTo. The copies converge when the OTel packages are rebased onto core (see spike notes).
internal static class ActivityIdExtensions
{
    private const int SpanIdByteCount = sizeof(long);
    private static readonly int TraceIdByteCount = Unsafe.SizeOf<Guid>();

    public static SpanId AsSentrySpanId(this ActivitySpanId id) => SpanId.Parse(id.ToHexString());

    public static ActivitySpanId AsActivitySpanId(this SpanId id)
    {
        Span<byte> buffer = stackalloc byte[SpanIdByteCount];
        id.TryWriteBytes(buffer);
        return ActivitySpanId.CreateFromBytes(buffer);
    }

    public static SentryId AsSentryId(this ActivityTraceId id) => SentryId.Parse(id.ToHexString());

    public static ActivityTraceId AsActivityTraceId(this SentryId id)
    {
        Span<byte> buffer = stackalloc byte[TraceIdByteCount];
        id.TryWriteBytes(buffer);
        return ActivityTraceId.CreateFromBytes(buffer);
    }
}
#endif
