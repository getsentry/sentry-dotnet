namespace Sentry;

/// <summary>
/// Terminal state of a session.
/// </summary>
public enum SessionEndStatus
{
    /// <summary>
    /// Session ended normally.
    /// </summary>
    Exited,

    /// <summary>
    /// Session ended with an unhandled exception.
    /// </summary>
    Unhandled,

    /// <summary>
    /// Session ended with a terminal unhandled exception.
    /// </summary>
    Crashed,

    /// <summary>
    /// Session ended abnormally (e.g. device lost power).
    /// </summary>
    Abnormal,
}
