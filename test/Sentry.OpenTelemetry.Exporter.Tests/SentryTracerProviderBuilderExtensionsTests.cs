using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry.Exporter.Tests;

public class SentryTracerProviderBuilderExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foo")]
    public void AddSentryOtlpExporter_InvalidDsn_ThrowsArgumentException(string dsn)
    {
        // Arrange
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        Action act = () => tracerProviderBuilder.AddSentryOtlpExporter(dsn);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"{SentryTracerProviderBuilderExtensions.MissingDsnWarning}*");
    }

    [Fact]
    public void OtlpConfigurationCallback_WithCustomCollectorUrl_SetsEndpointToCustomUrl()
    {
        // Arrange
        var dsn = Dsn.Parse("https://examplePublicKey@o0.ingest.sentry.io/123456");
        var customUrl = new Uri("https://custom-collector.example.com/api/traces");
        var options = new OtlpExporterOptions();

        // Act
        SentryTracerProviderBuilderExtensions.OtlpConfigurationCallback(options, customUrl, dsn.PublicKey);

        // Assert
        options.Endpoint.Should().Be(customUrl);
    }

    [Fact]
    public void OtlpConfigurationCallback_SetsEndpointFromDsn()
    {
        // Arrange
        var dsn = Dsn.Parse("https://examplePublicKey@o0.ingest.sentry.io/123456");
        var options = new OtlpExporterOptions();

        // Act
        SentryTracerProviderBuilderExtensions.OtlpConfigurationCallback(options, dsn.GetOtlpTracesEndpointUri(), dsn.PublicKey);

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
        SentryTracerProviderBuilderExtensions.OtlpConfigurationCallback(options, dsn.GetOtlpTracesEndpointUri(), dsn.PublicKey);

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
        SentryTracerProviderBuilderExtensions.OtlpConfigurationCallback(options, dsn.GetOtlpTracesEndpointUri(), dsn.PublicKey);
        var client = options.HttpClientFactory!.Invoke();

        // Assert
        client.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-Sentry-Auth");
        var headerValues = client.DefaultRequestHeaders.GetValues("X-Sentry-Auth");
        headerValues.Should().ContainSingle(v =>
            v == $"Sentry sentry_version={SentryConstants.ProtocolVersion}," +
                 $"sentry_client={SdkVersion.Instance.Name}/{SdkVersion.Instance.Version}," +
                 $"sentry_key={dsn.PublicKey}");
    }
}
