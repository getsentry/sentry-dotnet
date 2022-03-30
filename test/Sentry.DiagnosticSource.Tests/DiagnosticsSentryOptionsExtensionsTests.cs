using Sentry.Internals.DiagnosticSource;

namespace Sentry.DiagnosticSource.Internals;

public class DiagnosticsSentryOptionsExtensionsTests
{
    [Fact]
    public void DisableDiagnosticListenerIntegration_RemovesDiagnosticSourceIntegration()
    {
        var options = new SentryOptions();
        options.DisableDiagnosticSourceIntegration();
        Assert.DoesNotContain(options.Integrations!,
            p => p.GetType() == typeof(SentryDiagnosticListenerIntegration));
    }
}
