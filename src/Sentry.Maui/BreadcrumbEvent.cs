namespace Sentry.Maui;

/// <summary>
/// Argument to the OnBreadcrumbCreateCallback
/// </summary>
public record BreadcrumbEvent
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
    public (string Key, string Value)[] ExtraData { get; }

    /// <summary>
    /// Creates a new BreadcrumbEvent
    /// </summary>
    public BreadcrumbEvent(
        object? sender,
        string eventName,
        params (string Key, string Value)[] extraData)
    {
        Sender = sender;
        EventName = eventName;
        ExtraData = extraData;
    }

    /// <summary>
    /// This constructor remains for backward compatibility.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventName"></param>
    /// <param name="extraData"></param>
    [Obsolete("Use the simpler constructor with params (string Key, string Value)[] extraData instead.")]
    public BreadcrumbEvent(
        object? sender,
        string eventName,
        IEnumerable<(string Key, string Value)>[] extraData) : this(sender, eventName, extraData.SelectMany(e => e).ToArray())
    {
    }
}
