namespace Sentry.EntityFramework.Tests
{
    public class DbInterceptionIntegrationTests
    {
        [Fact]
        public void Register_TraceSAmpleRateZero_IntegrationNotRegistered()
        {
            // Arrange
            var logger = Substitute.For<ITestOutputHelper>();
            var options = new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = new TestOutputDiagnosticLogger(logger, SentryLevel.Debug),
                TracesSampleRate = 0
            };
            var integration = new DbInterceptionIntegration();

            // Act
            integration.Register(Substitute.For<IHub>(), options);

            // Assert
            logger.Received(1).WriteLine(Arg.Is<string>(message => message.Contains(DbInterceptionIntegration.SampleRateDisabledMessage)));
            Assert.Null(integration.SqlInterceptor);
        }
    }
}
