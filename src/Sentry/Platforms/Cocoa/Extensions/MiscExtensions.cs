namespace Sentry.Cocoa.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this CocoaSdk.SentryObjCId sentryId) => new(Guid.Parse(sentryId.SentryIdString));

    public static SpanId ToSpanId(this CocoaSdk.SentryObjCSpanId spanId) => new(spanId.SentrySpanIdString);

    public static CocoaSdk.SentryObjCId ToCocoaObjCId(this SentryId sentryId) => new(sentryId.ToString());

    public static CocoaSdk.SentryObjCSpanId ToCocoaObjCSpanId(this SpanId spanId) => new(spanId.ToString());
}
