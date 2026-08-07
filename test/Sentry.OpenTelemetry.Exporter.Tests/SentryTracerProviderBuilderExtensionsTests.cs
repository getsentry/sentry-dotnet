using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry.Exporter.Tests;

public class SentryTracerProviderBuilderExtensionsTests
{
    [Theory]
    [InlineData(null)]
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
    public void AddSentryOtlpExporter_DisabledSdkDsn_ReturnsWithoutConfiguringExporter()
    {
        // Arrange
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        var result = tracerProviderBuilder.AddSentryOtlpExporter(SentryConstants.DisableSdkDsnValue);

        // Assert
        result.Should().BeSameAs(tracerProviderBuilder);
        tracerProviderBuilder.DidNotReceive().AddInstrumentation(Arg.Any<Func<object>>());
    }

    [Fact]
    public void AddSentryOtlpExporter_DoesNotConfigureDefaultPropagator()
    {
        // Arrange
        // The OTLP integration spec requires that we MUST NOT set up an automatic propagator. Cross-service trace
        // propagation is left to the propagateTraceparent option or to user-configured OTel propagators.
        // See https://develop.sentry.dev/sdk/telemetry/traces/otlp/#integration-spec
        var sentinel = new TraceContextPropagator();
        Sdk.SetDefaultTextMapPropagator(sentinel);
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        tracerProviderBuilder.AddSentryOtlpExporter("https://examplePublicKey@o0.ingest.sentry.io/123456");

        // Assert
        Propagators.DefaultTextMapPropagator.Should().BeSameAs(sentinel);
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
