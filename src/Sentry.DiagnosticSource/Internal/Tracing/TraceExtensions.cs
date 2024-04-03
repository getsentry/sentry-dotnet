namespace Sentry.Internal.Tracing;

internal static class TraceExtensions
{
    private const string SentrySpanKey = "_Sentry.SentrySpan";
    private const string SentryExceptionKey = "_Sentry.Exception";

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

    public static void BindException(this System.Diagnostics.Activity activity, Exception exception)
    {
        activity.SetCustomProperty(SentryExceptionKey, exception);
    }

    public static Exception? GetException(this System.Diagnostics.Activity activity) =>
        activity.GetCustomProperty(SentryExceptionKey) as Exception;

    public static void BindSentrySpan(this System.Diagnostics.Activity activity, ISpan span)
    {
        activity.SetCustomProperty(SentrySpanKey, span);
        span.SetFused(activity); // We use a weak reference to allow the Activity to be disposed
    }

    public static ISpan? GetSentrySpan(this System.Diagnostics.Activity activity)
        => activity.GetCustomProperty(SentrySpanKey) is ISpan span ? span : null;

    public static System.Diagnostics.Activity? GetActivity(this ISpan span) => span.GetFused<System.Diagnostics.Activity>();
}
