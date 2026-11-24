namespace Sentry.Android.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this JavaSdk.Protocol.SentryId sentryId) => new(Guid.Parse(sentryId.ToString()));

    public static SpanId ToSpanId(this JavaSdk.SpanId spanId) => new(spanId.ToString());
}
