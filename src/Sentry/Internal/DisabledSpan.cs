namespace Sentry.Internal;

internal class DisabledSpan : ISentrySpan
{
    private static readonly Lazy<DisabledSpan> LazyInstance = new();
    public static DisabledSpan Instance => LazyInstance.Value;

    public void Dispose()
    {
        // No-Op
    }

    public void SetAttribute(string key, object value)
    {
        // No-Op
    }

    public void AddEvent(string message)
    {
        // No-Op
    }

    public void SetStatus(SpanStatus status, string? description = default)
    {
        // No-Op
    }

    public void Stop()
    {
        // No-Op
    }
}
