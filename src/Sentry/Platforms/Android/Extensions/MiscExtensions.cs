namespace Sentry.Android.Extensions;

internal static class SMiscExtensions
{
    public static SentryId ToSentryId(this Java.Protocol.SentryId sentryId) => new(Guid.Parse(sentryId.ToString()));

    public static Java.Protocol.SentryId ToJavaSentryId(this SentryId sentryId) => new(sentryId.ToString());

    public static SpanId ToSpanId(this Java.SpanId spanId) => new(spanId.ToString());

    public static Java.SpanId ToJavaSpanId(this SpanId spanId) => new(spanId.ToString());
}
