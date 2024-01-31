namespace Sentry;

/// <summary>
/// The level of the event sent to Sentry.
/// </summary>
public enum SentryLevel : short
{
    /// <summary>
    /// Debug.
    /// </summary>
    [EnumMember(Value = "debug")]
    Debug,

    /// <summary>
    /// Informational.
    /// </summary>
    [EnumMember(Value = "info")]
    Info,

    /// <summary>
    /// Warning.
    /// </summary>
    [EnumMember(Value = "warning")]
    Warning,

    /// <summary>
    /// Error.
    /// </summary>
    [EnumMember(Value = "error")]
    Error,

    /// <summary>
    /// Fatal.
    /// </summary>
    [EnumMember(Value = "fatal")]
    Fatal
}
