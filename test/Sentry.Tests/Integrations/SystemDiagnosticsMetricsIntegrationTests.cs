#if NET8_0_OR_GREATER
namespace Sentry.Tests.Integrations;

public class SystemDiagnosticsMetricsIntegrationTests
{
    [Fact]
    public void Register_NoListenersConfigured_LogsDisabledMessage()
    {
        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureSystemDiagnosticsInstruments = [],
                CaptureSystemDiagnosticsMeters = []
            }
        };
        var integration = new SystemDiagnosticsMetricsIntegration();

        // Act
        integration.Register(null!, options);

        // Assert
        logger.Received(1).Log(SentryLevel.Info, SystemDiagnosticsMetricsIntegration.NoListenersAreConfiguredMessage, null);
    }

    [Fact]
    public void Register_CaptureSystemDiagnosticsInstruments_Succeeds()
    {
        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        var options = new SentryOptions
        {
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureSystemDiagnosticsInstruments = [".*"],
                CaptureSystemDiagnosticsMeters = []
            }
        };
        var initializeDefaultListener = Substitute.For<Action<ExperimentalMetricsOptions>>();
        var integration = new SystemDiagnosticsMetricsIntegration(initializeDefaultListener);

        // Act
        integration.Register(null!, options);

        // Assert
        initializeDefaultListener.Received(1)(options.ExperimentalMetrics);
    }

    [Fact]
    public void Register_CaptureSystemDiagnosticsMeters_Succeeds()
    {
        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        var options = new SentryOptions
        {
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureSystemDiagnosticsInstruments = [],
                CaptureSystemDiagnosticsMeters = [".*"]
            }
        };
        var initializeDefaultListener = Substitute.For<Action<ExperimentalMetricsOptions>>();
        var integration = new SystemDiagnosticsMetricsIntegration(initializeDefaultListener);

        // Act
        integration.Register(null!, options);

        // Assert
        initializeDefaultListener.Received(1)(options.ExperimentalMetrics);
    }
}
#endif
