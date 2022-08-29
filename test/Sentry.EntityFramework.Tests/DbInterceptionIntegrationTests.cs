namespace Sentry.EntityFramework.Tests;

public class DbInterceptionIntegrationTests
{
    private readonly IDiagnosticLogger _logger;

    public DbInterceptionIntegrationTests(ITestOutputHelper output)
    {
        _logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(output, SentryLevel.Debug);
    }

    [Fact]
    public void Register_TraceSAmpleRateZero_IntegrationNotRegistered()
    {
        // Arrange
        var options = new SentryOptions
        {
            Debug = true,
            DiagnosticLogger = _logger,
            TracesSampleRate = 0
        };
        var integration = new DbInterceptionIntegration();

        // Act
        integration.Register(Substitute.For<IHub>(), options);

        // Assert
        _logger.Received(1).Log(
            Arg.Is(SentryLevel.Info),
            Arg.Is<string>(message => message.Contains(DbInterceptionIntegration.SampleRateDisabledMessage)));

        Assert.Null(integration.SqlInterceptor);
    }
}
