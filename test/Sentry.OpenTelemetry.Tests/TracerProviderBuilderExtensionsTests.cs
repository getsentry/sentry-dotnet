namespace Sentry.OpenTelemetry.Tests;

public class TracerProviderBuilderExtensionsTests
{
    private class Fixture
    {
        public IServiceProvider ServiceProvider { get; } = Substitute.For<IServiceProvider>();
        public IHub Hub { get; } = Substitute.For<IHub>();

        public Fixture()
        {
            ServiceProvider.GetService(typeof(IHub)).Returns(Hub);
        }

        public SentryOptions GetOptions(string dsn = "https://123@o456.ingest.sentry.io/789") => new()
        {
            Instrumenter = Instrumenter.OpenTelemetry,
            Dsn = dsn
        };

        public IServiceProvider GetServiceProvider() => ServiceProvider;
    }


    [Fact]
    public void ImplementationFactory_WithUserFactory_AddsAspNetCoreEnricher()
    {
        // Arrange
        var fixture = new Fixture();
        SentryClientExtensions.SentryOptionsForTestingOnly = fixture.GetOptions();
        fixture.Hub.IsEnabled.Returns(true);
        var userFactory = Substitute.For<ISentryUserFactory>();
        fixture.ServiceProvider.GetService(typeof(ISentryUserFactory)).Returns(userFactory);
        var services = fixture.GetServiceProvider();

        // Act
        var result = TracerProviderBuilderExtensions.ImplementationFactory(services);

        // Assert
        result.Should().BeOfType<SentrySpanProcessor>(); // FluentAssertions
        var spanProcessor = (SentrySpanProcessor)result;
        spanProcessor._enrichers.Should().NotBeEmpty();
    }

    [Fact]
    public void ImplementationFactory_WithoutUserFactory_DoesNotAddAspNetCoreEnricher()
    {
        // Arrange
        var fixture = new Fixture();
        SentryClientExtensions.SentryOptionsForTestingOnly = fixture.GetOptions();
        fixture.Hub.IsEnabled.Returns(true);
        var services = fixture.GetServiceProvider();

        // Act
        var result = TracerProviderBuilderExtensions.ImplementationFactory(services);

        // Assert
        result.Should().BeOfType<SentrySpanProcessor>(); // FluentAssertions
        var spanProcessor = (SentrySpanProcessor)result;
        spanProcessor._enrichers.Should().BeEmpty();
    }

    [Fact]
    public void ImplementationFactory_WithEnabledHub_ReturnsSentrySpanProcessor()
    {
        // Arrange
        var fixture = new Fixture();
        SentryClientExtensions.SentryOptionsForTestingOnly = fixture.GetOptions();
        fixture.Hub.IsEnabled.Returns(true);
        var services = fixture.GetServiceProvider();

        // Act
        var result = TracerProviderBuilderExtensions.ImplementationFactory(services);

        // Assert
        result.Should().BeOfType<SentrySpanProcessor>(); // FluentAssertions
    }

    [Fact]
    public void ImplementationFactory_WithDisabledHub_ReturnsDisabledSpanProcessor()
    {
        // Arrange
        var fixture = new Fixture();
        SentryClientExtensions.SentryOptionsForTestingOnly = fixture.GetOptions();
        fixture.Hub.IsEnabled.Returns(false);
        var services = fixture.GetServiceProvider();

        // Act
        var result = TracerProviderBuilderExtensions.ImplementationFactory(services);

        // Assert
        result.Should().BeOfType<DisabledSpanProcessor>(); // FluentAssertions
    }
}
