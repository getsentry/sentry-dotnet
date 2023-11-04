using Sentry.Extensibility;
using Sentry.Internal.DiagnosticSource;

namespace Sentry;

/// <summary>
/// The additional Sentry Options extensions from Sentry Diagnostic Listener.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryOptionsDiagnosticExtensions
{
    /// <summary>
    /// Attach Sentry to System DiagnosticSource.
    /// </summary>
    /// <param name="options">The Sentry options.</param>
    public static void AddDiagnosticSourceIntegration(this SentryOptions options)
    {
        if (options.HasDefaultIntegration(SentryOptions.DefaultIntegrations.SentryDiagnosticListenerIntegration))
        {
            options.LogWarning($"{nameof(SentryDiagnosticListenerIntegration)} has already been added. The second call to {nameof(AddDiagnosticSourceIntegration)} will be ignored.");
            return;
        }

        options.AddIntegration(new SentryDiagnosticListenerIntegration());
    }

    /// <summary>
    /// Disables the integrations with Diagnostic source.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableDiagnosticSourceIntegration(this SentryOptions options)
        => options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.SentryDiagnosticListenerIntegration);
}
