namespace Sentry.Protocol;

/// <summary>
/// Indicates how a trace or span was created
/// </summary>
internal enum OriginType
{
    /// <summary>
    /// Indicates the trace or span was created by the SDK or some integration
    /// </summary>
    Auto,
    /// <summary>
    /// Indicates that the user created the trace or span
    /// </summary>
    Manual
}
