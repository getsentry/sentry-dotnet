namespace Sentry.Extensibility;

/// <summary>
/// Extensions that let integrations write to the SDK's diagnostic logger.
/// </summary>
public static class HubDiagnosticExtensions
{
    /// <summary>
    /// Gets the <see cref="SentryOptions.DiagnosticLogger"/> the SDK was initialized with.
    /// </summary>
    /// <param name="hub">The hub.</param>
    /// <returns>
    /// The diagnostic logger, or <c>null</c> if <see cref="SentryOptions.Debug"/> is off, the SDK hasn't been
    /// initialized, or the hub isn't one created by the SDK.
    /// </returns>
    public static IDiagnosticLogger? GetDiagnosticLogger(this IHub hub) =>
        hub.GetSentryOptions()?.DiagnosticLogger;
}
