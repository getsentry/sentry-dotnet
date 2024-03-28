namespace Sentry.Internal.Tracing;

internal class SentryTraceSpan : ITraceSpan
{
    private readonly ISpan _span;
    private Scope? _scope;

    public SentryTraceSpan(IHub hub, ISpan span)
    {
        _span = span;
        hub.ConfigureScope(scope => _scope = scope);
    }

    public void Dispose()
    {
        // ISpan doesn't implement IDisposable
    }

    public void SetAttribute(string key, object value)
    {
        var stringValue = $"{value}";
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            _span.UnsetTag(key);
            return;
        }
        _span.SetTag(key, stringValue);
    }

    public void AddEvent(string message)
    {
        _scope?.AddBreadcrumb(message);
    }

    public void SetStatus(SpanStatus status, string? description = default)
    {
        _span.Status = status;
        if (_span.Status != SpanStatus.Ok)
        {
            _span.Description = description;
        }
    }

    public void Stop()
    {
        _span.Finish();
    }
}
