using Sentry.Internals.DiagnosticSource;

namespace Sentry;

/// <summary>
/// The additional Sentry Options extensions from Sentry Diagnostic Listener.
/// </summary>
public static class SentryOptionsDiagnosticExtensions
{
    /// <summary>
    /// Attach Sentry to System DiagnosticSource.
    /// </summary>
    /// <param name="options">The Sentry options.</param>
    public static void AddDiagnosticSourceIntegration(this SentryOptions options)
        => options.AddIntegration(new SentryDiagnosticListenerIntegration());

    /// <summary>
    /// Disables the integrations with Diagnostic source.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableDiagnosticSourceIntegration(this SentryOptions options)
        => options.Integrations =
            options.Integrations?.Where(p => p.GetType() != typeof(SentryDiagnosticListenerIntegration)).ToArray();
}
