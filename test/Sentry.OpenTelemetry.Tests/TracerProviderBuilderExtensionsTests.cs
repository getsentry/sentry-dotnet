using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foo")]
    public void AddSentryOltp_InvalidDsn_ThrowsArgumentException(string dsn)
    {
        // Arrange
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        Action act = () => tracerProviderBuilder.AddSentryOtlp(dsn);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"{TracerProviderBuilderExtensions.MissingDsnWarning}*");
    }

    [Fact]
    public void OtlpConfigurationCallback_SetsEndpointFromDsn()
    {
        // Arrange
        var dsn = Dsn.Parse("https://examplePublicKey@o0.ingest.sentry.io/123456");
        var options = new OtlpExporterOptions();

        // Act
        TracerProviderBuilderExtensions.OtlpConfigurationCallback(options, dsn);

        // Assert
        options.Endpoint.Should().Be(dsn.GetOtlpTracesEndpointUri());
    }

    [Fact]
    public void OtlpConfigurationCallback_SetsProtocolToHttpProtobuf()
    {
        // Arrange
        var dsn = Dsn.Parse("https://examplePublicKey@o0.ingest.sentry.io/123456");
        var options = new OtlpExporterOptions();

        // Act
        TracerProviderBuilderExtensions.OtlpConfigurationCallback(options, dsn);

        // Assert
        options.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
    }

    [Fact]
    public void OtlpConfigurationCallback_HttpClientFactoryCreatesClientWithSentryAuthHeader()
    {
        // Arrange
        var dsn = Dsn.Parse("https://examplePublicKey@o0.ingest.sentry.io/123456");
        var options = new OtlpExporterOptions();

        // Act
        TracerProviderBuilderExtensions.OtlpConfigurationCallback(options, dsn);
        var client = options.HttpClientFactory!.Invoke();

        // Assert
        client.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-Sentry-Auth");
        var headerValues = client.DefaultRequestHeaders.GetValues("X-Sentry-Auth");
        headerValues.Should().ContainSingle(v => v == $"sentry sentry_key={dsn.PublicKey}");
    }
}
