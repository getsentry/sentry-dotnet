namespace Sentry;

/// <summary>
/// Constant values.
/// </summary>
public static class SentryConstants
{
    /// <summary>
    /// Empty string disables the SDK.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/overview/#usage-for-end-users"/>
    public const string DisableSdkDsnValue = "";

    /// <summary>
    /// Default maximum number of breadcrumbs to hold in memory.
    /// </summary>
    public const int DefaultMaxBreadcrumbs = 100;

    /// <summary>
    /// Protocol version.
    /// </summary>
    public const int ProtocolVersion = 7;

    /// <summary>
    /// Platform key that defines an events is coming from any .NET implementation.
    /// </summary>
    public const string Platform = "csharp";
}
