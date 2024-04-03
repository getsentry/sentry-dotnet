namespace Sentry.Internal.Tracing;

internal interface ITraceSpan : IDisposable
{
    string? Description { get; }
    ITraceSpan AddEvent(string message);
    ITraceSpan SetAttribute(string key, object value);
    ITraceSpan SetDescription(string? description);
    ITraceSpan SetStatus(SpanStatus status, string? description = default);
    ITraceSpan Stop();

    ITraceSpan SetExtra(string key, object? value);

    ITraceSpan Finish(Exception exception);
}

internal static class TraceSpanExtensions
{
    internal static ITraceSpan SetExtras(this ITraceSpan traceSpan, IEnumerable<KeyValuePair<string, object?>> values)
    {
        foreach (var (key, value) in values)
        {
            traceSpan.SetExtra(key, value);
        }

        return traceSpan;
    }
}
