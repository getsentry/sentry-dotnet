namespace Sentry.Cocoa.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this CocoaSdk.SentryId sentryId) => new(Guid.Parse(sentryId.SentryIdString));

    public static SpanId ToSpanId(this CocoaSdk.SentrySpanId spanId) => new(spanId.SentrySpanIdString);

    // The SentryObjCSDK.internal hybrid API uses its own id types (SentryObjCId / SentryObjCSpanId)
    // rather than the Sentry.framework SentryId / SentrySpanId used elsewhere.
    public static CocoaSdk.SentryObjCId ToCocoaObjCId(this SentryId sentryId) => new(sentryId.ToString());

    public static CocoaSdk.SentryObjCSpanId ToCocoaObjCSpanId(this SpanId spanId) => new(spanId.ToString());
}
