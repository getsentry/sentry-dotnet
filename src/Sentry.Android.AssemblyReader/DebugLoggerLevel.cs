namespace Sentry.Android.AssemblyReader;

/// <summary>
/// Represents the level of debug logging.
/// </summary>
public enum DebugLoggerLevel : short
{
    /// <summary>
    /// Debug level logging.
    /// </summary>
    Debug,

    /// <summary>
    /// Information level logging.
    /// </summary>
    Info,

    /// <summary>
    /// Warning level logging.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level logging.
    /// </summary>
    Error,

    /// <summary>
    /// Fatal level logging.
    /// </summary>
    Fatal
}
