namespace Sentry.Android.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this JavaSdk.Protocol.SentryId sentryId) => new(Guid.Parse(sentryId.ToString()));

    public static JavaSdk.Protocol.SentryId ToJavaSentryId(this SentryId sentryId) => new(sentryId.ToString());

    public static SpanId ToSpanId(this JavaSdk.SpanId spanId) => new(spanId.ToString());

    public static JavaSdk.SpanId ToJavaSpanId(this SpanId spanId) => new(spanId.ToString());
}
