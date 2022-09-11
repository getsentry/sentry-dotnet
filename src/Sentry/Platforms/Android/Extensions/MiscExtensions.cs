namespace Sentry.Android.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this Java.Protocol.SentryId sentryId) => new(Guid.Parse(sentryId.ToString()));

    public static Java.Protocol.SentryId ToJavaSentryId(this SentryId sentryId) => new(sentryId.ToString());

    public static SpanId ToSpanId(this Java.SpanId spanId) => new(spanId.ToString());

    public static TransactionNameSource ToSentryTransactionNameSource(this Sentry.Java.Protocol.TransactionNameSource source)
    {
        if (source == Java.Protocol.TransactionNameSource.Custom)
        {
            return TransactionNameSource.Custom;
        }

        if (source == Java.Protocol.TransactionNameSource.Url)
        {
            return TransactionNameSource.Url;
        }

        if (source == Java.Protocol.TransactionNameSource.Route)
        {
            return TransactionNameSource.Route;
        }

        if (source == Java.Protocol.TransactionNameSource.View)
        {
            return TransactionNameSource.View;
        }

        if (source == Java.Protocol.TransactionNameSource.Task)
        {
            return TransactionNameSource.Task;
        }

        throw new($"Unknown TransactionNameSource: {source.Name()}");
    }

    public static Java.SpanId ToJavaSpanId(this SpanId spanId) => new(spanId.ToString());
}
