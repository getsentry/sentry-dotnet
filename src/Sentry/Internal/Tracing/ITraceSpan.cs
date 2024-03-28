namespace Sentry.Internal.Tracing;

internal interface ITraceSpan : IDisposable
{
    void SetAttribute(string key, object value);
    void AddEvent(string message);
    void SetStatus(SpanStatus status, string? description = default);
    void Stop();
}
