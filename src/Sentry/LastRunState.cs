namespace Sentry;

/// <summary>
/// Represents the crash state of the games's previous run.
/// Used to determine if the last execution terminated normally or crashed.
/// </summary>
public enum CrashedLastRun
{
    /// <summary>
    /// The LastRunState is unknown. This might be due to the SDK not being initialized, native crash support
    /// missing, or being disabled.
    /// </summary>
    Unknown,

    /// <summary>
    /// The application did not crash during the last run.
    /// </summary>
    DidNotCrash,

    /// <summary>
    /// The application crashed during the last run.
    /// </summary>
    Crashed
}
