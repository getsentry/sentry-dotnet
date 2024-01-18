#if NET8_0_OR_GREATER
namespace Sentry.Tests.Integrations;

public class SystemDiagnosticsMetricsIntegrationTests
{
    [Fact]
    public void Register_CaptureInstrumentsNotConfigured_LogsDisabledMessage()
    {
        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
        };
        var integration = new SystemDiagnosticsMetricsIntegration();

        // Act
        integration.Register(null!, options);

        // Assert
        logger.Received(1).Log(SentryLevel.Info, SystemDiagnosticsMetricsIntegration.NoListenersAreConfiguredMessage, null);
    }

    [Fact]
    public void Register_Net8OrGreater_LogsDisabledMessage()
    {
        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        var options = new SentryOptions
        {
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureInstruments = [".*"]
            }
        };
        var initializeDefaultListener = Substitute.For<Action<IEnumerable<SubstringOrRegexPattern>>>();
        var integration = new SystemDiagnosticsMetricsIntegration(initializeDefaultListener);

        // Act
        integration.Register(null!, options);

        // Assert
        initializeDefaultListener.Received(1)(options.ExperimentalMetrics.CaptureInstruments);
    }
}
#endif
