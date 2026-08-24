namespace Sentry.Maui;

/// <summary>
/// Argument to the OnBreadcrumbCreateCallback
/// </summary>
public sealed class BreadcrumbEvent
{
    /// <summary>
    /// The sender of the event, usually the control that triggered it.
    /// </summary>
    public object? Sender { get; }

    /// <summary>
    /// The event name (e.g. "Tapped", "Swiped", etc.)
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Any extra data to be included in the breadcrumb. This would typically be event specific information (for example
    /// it could include the X, Y coordinates of a tap event).
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> ExtraData { get; }

    /// <summary>
    /// Creates a new BreadcrumbEvent
    /// </summary>
    public BreadcrumbEvent(object? sender, string eventName)
        : this(sender, eventName, Array.Empty<KeyValuePair<string, string>>())
    {
    }

    /// <summary>
    /// Creates a new BreadcrumbEvent
    /// </summary>
    public BreadcrumbEvent(
        object? sender,
        string eventName,
        params IEnumerable<KeyValuePair<string, string>> extraData)
    {
        Sender = sender;
        EventName = eventName;
        ExtraData = extraData;
    }

    /// <summary>
    /// Creates a new BreadcrumbEvent
    /// </summary>
    public BreadcrumbEvent(
        object? sender,
        string eventName,
        params IEnumerable<(string key, string value)> extraData) : this(sender, eventName, extraData.Select(
            e => new KeyValuePair<string, string>(e.key, e.value)))
    {
    }
}
