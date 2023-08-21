using System.Runtime.Versioning;

namespace Sentry;

/// <summary>
/// The mode of which to attempt to detect the process startup time.
/// </summary>
public enum StartupTimeDetectionMode
{
    /// <summary>
    /// Disabled.
    /// </summary>
    None,
    /// <summary>
    /// Best effort approach that can be off by a few seconds or minutes.
    /// </summary>
    /// <remarks>
    /// In this mode, the App startup time is assumed to be the point of which the SDK was initialized.
    /// </remarks>
    Fast,
    /// <summary>
    /// Attempts to detect the startup time with the most precision.
    /// </summary>
    /// <remarks>
    /// This can require starting work on the thread pool due to P/Invoke calls.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    Best,
}
