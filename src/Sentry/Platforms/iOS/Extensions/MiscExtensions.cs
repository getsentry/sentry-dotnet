namespace Sentry.iOS.Extensions;

internal static class MiscExtensions
{
    public static SentryId ToSentryId(this SentryCocoa.SentryId sentryId) => new(Guid.Parse(sentryId.SentryIdString));

    public static SentryCocoa.SentryId ToCocoaSentryId(this SentryId sentryId) => new(sentryId.ToString());

    public static SpanId ToSpanId(this SentryCocoa.SentrySpanId spanId) => new(spanId.SentrySpanIdString);

    public static TransactionNameSource ToSentryTransactionNameSource(this Sentry.Cocoa.Protocol.TransactionNameSource source)
    {
        if (source == Cocoa.Protocol.TransactionNameSource.Custom)
        {
            return TransactionNameSource.Custom;
        }

        if (source == Cocoa.Protocol.TransactionNameSource.Url)
        {
            return TransactionNameSource.Url;
        }

        if (source == Cocoa.Protocol.TransactionNameSource.Route)
        {
            return TransactionNameSource.Route;
        }

        if (source == Cocoa.Protocol.TransactionNameSource.View)
        {
            return TransactionNameSource.View;
        }

        if (source == Cocoa.Protocol.TransactionNameSource.Task)
        {
            return TransactionNameSource.Task;
        }

        throw new($"Unknown TransactionNameSource: {source.Name()}");
    }

    public static SentryCocoa.SentrySpanId ToCocoaSpanId(this SpanId spanId) => new(spanId.ToString());
}
