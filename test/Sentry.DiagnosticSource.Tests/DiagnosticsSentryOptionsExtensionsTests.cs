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
            p => p is SentryDiagnosticListenerIntegration);
    }

#if NETCOREAPP3_0 || NETCOREAPP2_1 || NET461
    [Fact]
    public void AddDiagnosticSourceIntegration()
    {
        var options = new SentryOptions();
        options.AddDiagnosticSourceIntegration();
        Assert.Contains(options.Integrations!,
            p => p is SentryDiagnosticListenerIntegration);
    }

    [Fact]
    public void AddDiagnosticSourceIntegration_NoDuplicates()
    {
        var options = new SentryOptions();
        options.AddDiagnosticSourceIntegration();
        options.AddDiagnosticSourceIntegration();
        Assert.Single(options.Integrations!,
            p => p is SentryDiagnosticListenerIntegration);
    }
#endif
}
