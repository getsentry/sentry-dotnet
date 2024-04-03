namespace Sentry.Internal.Tracing;

internal class SentryTraceSpan : ITraceSpan
{
    private readonly ISpan _span;
    private Scope? _scope;

    public string? Description => _span.Description;

    public SentryTraceSpan(IHub hub, ISpan span)
    {
        _span = span;
        hub.ConfigureScope(scope => _scope = scope);
    }

    public void Dispose()
    {
        // ISpan doesn't implement IDisposable
    }

    public ITraceSpan AddEvent(string message)
    {
        _scope?.AddBreadcrumb(message);
        return this;
    }

    public ITraceSpan SetAttribute(string key, object value)
    {
        var stringValue = $"{value}";
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            _span.UnsetTag(key);
        }
        else
        {
            _span.SetTag(key, stringValue);
        }
        return this;
    }

    public ITraceSpan SetDescription(string? description)
    {
        _span.Description = description;
        return this;
    }

    public ITraceSpan SetStatus(SpanStatus status, string? description = default)
    {
        _span.Status = status;
        if (_span.Status != SpanStatus.Ok)
        {
            _span.Description = description;
        }
        return this;
    }

    public ITraceSpan Stop()
    {
        _span.Finish();
        return this;
    }

    public ITraceSpan SetExtra(string key, object? value)
    {
        _span.SetExtra(key, value);
        return this;
    }

    public ITraceSpan Finish(Exception exception)
    {
        _span.Finish(exception);
        return this;
    }
}
