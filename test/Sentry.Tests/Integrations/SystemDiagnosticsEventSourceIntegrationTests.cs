#if !__MOBILE__
using Sentry.PlatformAbstractions;

namespace Sentry.Tests.Integrations;

public class SystemDiagnosticsEventSourceIntegrationTests
{
    [SkippableFact]
    public void Register_NoListenersConfigured_LogsDisabledMessage()
    {
        Skip.If(RuntimeInfo.GetRuntime().IsMono());

        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureSystemDiagnosticsEventSources = []
            }
        };
        var integration = new SystemDiagnosticsEventSourceIntegration();

        // Act
        integration.Register(null!, options);

        // Assert
        logger.Received(1).Log(SentryLevel.Info, SystemDiagnosticsEventSourceIntegration.NoListenersAreConfiguredMessage, null);
    }

    [SkippableFact]
    public void Register_IsMono_LogsDisabledMessage()
    {
        Skip.IfNot(RuntimeInfo.GetRuntime().IsMono());

        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureSystemDiagnosticsEventSources = [".*"]
            }
        };
        var initializeDefaultListener = Substitute.For<Action<ExperimentalMetricsOptions>>();
        var integration = new SystemDiagnosticsEventSourceIntegration(initializeDefaultListener);

        // Act
        integration.Register(null!, options);

        // Assert
        logger.Received(1).Log(SentryLevel.Info, SystemDiagnosticsEventSourceIntegration.MonoNotSupportedMessage, null);
    }

    [SkippableFact]
    public void Register_Listeners_Succeeds()
    {
        Skip.If(RuntimeInfo.GetRuntime().IsMono());

        // Arrange
        var logger = Substitute.For<IDiagnosticLogger>();
        var options = new SentryOptions
        {
            DiagnosticLogger = logger,
            ExperimentalMetrics = new ExperimentalMetricsOptions()
            {
                CaptureSystemDiagnosticsEventSources = [".*"]
            }
        };
        var initializeDefaultListener = Substitute.For<Action<ExperimentalMetricsOptions>>();
        var integration = new SystemDiagnosticsEventSourceIntegration(initializeDefaultListener);

        // Act
        integration.Register(null!, options);

        // Assert
        initializeDefaultListener.Received(1)(options.ExperimentalMetrics);
    }
}
#endif
