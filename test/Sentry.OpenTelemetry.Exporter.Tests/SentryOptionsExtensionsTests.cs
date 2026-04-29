using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Sentry.OpenTelemetry.Exporter.Tests;

public class SentryOptionsExtensionsTests
{
    [Fact]
    public void UseOtlp_SetsInstrumenterToOpenTelemetry()
    {
        // Arrange
        var options = new SentryOptions { Dsn = ValidDsn };

        // Act
        options.UseOtlp();

        // Assert
        options.Instrumenter.Should().Be(Instrumenter.OpenTelemetry);
    }

    [Fact]
    public void UseOtlp_DisablesSentryTracing()
    {
        // Arrange
        var options = new SentryOptions { Dsn = ValidDsn };

        // Act
        options.UseOtlp();

        // Assert
        options.DisableSentryTracing.Should().BeTrue();
    }

    [Fact]
    public void UseOtlp_SetsExternalPropagationContextToOtelPropagationContext()
    {
        // Arrange
        var options = new SentryOptions { Dsn = ValidDsn };

        // Act
        options.UseOtlp();

        // Assert
        options.ExternalPropagationContext.Should().NotBeNull();
        options.ExternalPropagationContext!.Should().BeOfType<OtelPropagationContext>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UseOtlp_WithTracerProviderBuilder_MissingDsn_ThrowsArgumentException(string dsn)
    {
        // Arrange
        var options = new SentryOptions { Dsn = dsn };
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        var act = () => options.UseOtlp(tracerProviderBuilder);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UseOtlp_WithTracerProviderBuilder_SetsInstrumenterToOpenTelemetry()
    {
        // Arrange
        var options = new SentryOptions { Dsn = ValidDsn };
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        options.UseOtlp(tracerProviderBuilder);

        // Assert
        options.Instrumenter.Should().Be(Instrumenter.OpenTelemetry);
    }

    [Fact]
    public void UseOtlp_WithTracerProviderBuilder_DisablesSentryTracing()
    {
        // Arrange
        var options = new SentryOptions { Dsn = ValidDsn };
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();

        // Act
        options.UseOtlp(tracerProviderBuilder);

        // Assert
        options.DisableSentryTracing.Should().BeTrue();
    }

    [Fact]
    public void UseOtlp_WithCustomPropagator_DoesNotThrow()
    {
        // Arrange
        var options = new SentryOptions { Dsn = DsnSamples.ValidDsn };
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();
        var customPropagator = Substitute.For<TextMapPropagator>();

        // Act
        var act = () => options.UseOtlp(tracerProviderBuilder, customPropagator);

        // Assert
        act.Should().NotThrow();
    }
}
