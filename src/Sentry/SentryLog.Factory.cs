namespace Sentry;

public sealed partial class SentryLog
{
    internal static SentryLog Create(IHub hub, DateTimeOffset timestamp, SentryLogLevel level, string message, string? template, ImmutableArray<KeyValuePair<string, object>> parameters)
    {
        hub.GetTraceIdAndSpanId(out var traceId, out var spanId);

        SentryLog log = new(timestamp, traceId, level, message)
        {
            Template = template,
            Parameters = parameters,
            SpanId = spanId,
        };

        return log;
    }
}
