namespace Sentry.Maui;

/// <summary>
/// Configures when to attach LogCat logs to events.
/// </summary>
public enum LogCatIntegrationType
{
    /// <summary>
    /// The LogCat integration is disabled.
    /// </summary>
    None,
    /// <summary>
    /// LogCat logs are attached to events only when the event is unhandled.
    /// </summary>
    Unhandled,

    /// <summary>
    /// LogCat logs are attached to all events.
    /// </summary>
    All
}
