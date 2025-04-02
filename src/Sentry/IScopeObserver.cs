namespace Sentry;

/// <summary>
/// Observer for the sync. of Scopes across SDKs.
/// </summary>
public interface IScopeObserver
{
    /// <summary>
    /// Adds a breadcrumb.
    /// </summary>
    public void AddBreadcrumb(Breadcrumb breadcrumb);

    /// <summary>
    /// Sets an extra.
    /// </summary>
    public void SetExtra(string key, object? value);

    /// <summary>
    /// Sets a tag.
    /// </summary>
    public void SetTag(string key, string value);

    /// <summary>
    /// Removes a tag.
    /// </summary>
    public void UnsetTag(string key);

    /// <summary>
    /// Sets the user information.
    /// </summary>
    public void SetUser(SentryUser? user);

    /// <summary>
    /// Sets the current trace
    /// </summary>
    public void SetTrace(SentryId traceId, SpanId parentSpanId);
}
