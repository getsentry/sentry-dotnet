using Sentry.Internals.DiagnosticSource;
using Xunit;

namespace Sentry.Diagnostics.DiagnosticSource.Internals
{
    public class DiagnosticsSentryOptionsExtensionsTests
    {
        public SentryOptions Sut { get; set; } = new();

        [Fact]
        public void DisableDiagnosticListnerIntegration_RemovesDiagnosticSourceIntegration()
        {
            Sut.DisableDiagnosticListenerIntegration();
            Assert.DoesNotContain(Sut.Integrations!,
                p => p.GetType() == typeof(SentryDiagnosticListenerIntegration));
        }
    }
}
