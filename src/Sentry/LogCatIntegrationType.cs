namespace Sentry;

#if ANDROID
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
    /// LogCat logs are attached to events with an exception.
    /// </summary>
    Errors,

    /// <summary>
    /// LogCat logs are attached to all events.
    /// Use caution when enabling, as this may result in a lot of data being sent to Sentry
    /// and performance issues if the SDK generates a lot of events.
    /// </summary>
    All
}
#endif
