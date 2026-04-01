using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol.Tests;

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
