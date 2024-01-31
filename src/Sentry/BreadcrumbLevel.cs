namespace Sentry;

/// <summary>
/// The level of the Breadcrumb.
/// </summary>
public enum BreadcrumbLevel
{
    /// <summary>
    /// Debug level.
    /// </summary>
    [EnumMember(Value = "debug")]
    Debug = -1,

    /// <summary>
    /// Information level.
    /// </summary>
    /// <remarks>
    /// This is value 0, hence, default.
    /// </remarks>
    [EnumMember(Value = "info")]
    Info = 0, // Defaults to Info

    /// <summary>
    /// Warning breadcrumb level.
    /// </summary>
    [EnumMember(Value = "warning")]
    Warning = 1,

    /// <summary>
    /// Error breadcrumb level.
    /// </summary>
    [EnumMember(Value = "error")]
    Error = 2,

    /// <summary>
    /// Critical breadcrumb level.
    /// </summary>
    [EnumMember(Value = "critical")]
    Critical = 3,
}
