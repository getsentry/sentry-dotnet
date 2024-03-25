namespace Sentry.Internal;

internal interface ISentrySpan : IDisposable
{
    void SetAttribute(string key, object value);
    void AddEvent(string message);
    void SetStatus(SpanStatus status, string? description = default);
    void Stop();
}
