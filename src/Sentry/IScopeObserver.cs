namespace Sentry;

/// <summary>
/// Observer for the sync. of Scopes across SDKs.
/// </summary>
public interface IScopeObserver
{
    /// <summary>
    /// Adds a breadcrumb.
    /// </summary>
    void AddBreadcrumb(Breadcrumb breadcrumb);

    /// <summary>
    /// Sets an extra.
    /// </summary>
    void SetExtra(string key, object? value);

    /// <summary>
    /// Sets a tag.
    /// </summary>
    void SetTag(string key, string value);

    /// <summary>
    /// Removes a tag.
    /// </summary>
    void UnsetTag(string key);

    /// <summary>
    /// Sets the user information.
    /// </summary>
    void SetUser(SentryUser? user);

    /// <summary>
    /// Sets the current trace
    /// </summary>
    void SetTrace(SentryId traceId, SpanId parentSpanId);
}
