namespace Sentry.Cocoa.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this CocoaSdk.SentryId sentryId) => new(Guid.Parse(sentryId.SentryIdString));

    public static CocoaSdk.SentryId ToCocoaSentryId(this SentryId sentryId) => new(sentryId.ToString());

    public static SpanId ToSpanId(this CocoaSdk.SentrySpanId spanId) => new(spanId.SentrySpanIdString);

    public static CocoaSdk.SentrySpanId ToCocoaSpanId(this SpanId spanId) => new(spanId.ToString());
}
