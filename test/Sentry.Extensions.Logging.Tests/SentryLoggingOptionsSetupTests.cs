using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingOptionsSetupTests
{
    [Fact]
    public void Configure_BindsConfigurationToOptions()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MinimumBreadcrumbLevel"] = nameof(LogLevel.Debug),
                ["MinimumEventLevel"] = nameof(LogLevel.Critical),
            })
            .Build();

        var providerConfig = Substitute.For<ILoggerProviderConfiguration<SentryLoggerProvider>>();
        providerConfig.Configuration.Returns(config);
        var actual = new SentryLoggingOptions();

        var setup = new SentryLoggingOptionsSetup(providerConfig);

        // Act
        setup.Configure(actual);

        // Assert
        using (new AssertionScope())
        {
            actual.MinimumBreadcrumbLevel.Should().Be(LogLevel.Debug);
            actual.MinimumEventLevel.Should().Be(LogLevel.Critical);
        }
    }
}
