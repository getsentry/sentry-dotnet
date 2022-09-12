namespace Sentry.iOS.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this SentryCocoa.SentryId sentryId) => new(Guid.Parse(sentryId.SentryIdString));

    public static SentryCocoa.SentryId ToCocoaSentryId(this SentryId sentryId) => new(sentryId.ToString());

    public static SpanId ToSpanId(this SentryCocoa.SentrySpanId spanId) => new(spanId.SentrySpanIdString);

    public static SentryCocoa.SentrySpanId ToCocoaSpanId(this SpanId spanId) => new(spanId.ToString());
}
