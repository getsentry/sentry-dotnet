using Sentry.Internals.DiagnosticSource;

namespace Sentry.DiagnosticSource.Internals;

public class DiagnosticsSentryOptionsExtensionsTests
{
    public SentryOptions Sut { get; set; } = new();

    [Fact]
    public void DisableDiagnosticListenerIntegration_RemovesDiagnosticSourceIntegration()
    {
        Sut.DisableDiagnosticSourceIntegration();
        Assert.DoesNotContain(Sut.Integrations!,
            p => p.GetType() == typeof(SentryDiagnosticListenerIntegration));
    }
}
