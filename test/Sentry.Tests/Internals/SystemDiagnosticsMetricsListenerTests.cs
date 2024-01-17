#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace Sentry.Tests.Internals;

public class SystemDiagnosticsMetricsListenerTests
{
    private class Fixture
    {
        public readonly IMetricAggregator MockAggregator = Substitute.For<IMetricAggregator>();
        public readonly List<SubstringOrRegexPattern> CaptureInstruments = new ();

        public SystemDiagnosticsMetricsListener GetSut()
        {
            return new SystemDiagnosticsMetricsListener(CaptureInstruments, MockAggregator);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void RecordMeasurement_CounterInstrument_CallsIncrement()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateCounter<int>("test.counter", "unit");
        const int measurement = 2;

        // Act
        var sut = _fixture.GetSut();
        sut.RecordMeasurement(instrument, measurement, ReadOnlySpan<KeyValuePair<string, object>>.Empty, null);

        // Assert
        _fixture.MockAggregator.Received().Increment(
            instrument.Name,
            measurement,
            MeasurementUnit.Custom(instrument.Unit!),
            Arg.Any<ImmutableDictionary<string, string>>()
            );
    }

    [Fact]
    public void RecordMeasurement_HistogramInstrument_CallsDistribution()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateHistogram<int>("test.histogram", "unit");
        const int measurement = 2;

        // Act
        var sut = _fixture.GetSut();
        sut.RecordMeasurement(instrument, measurement, ReadOnlySpan<KeyValuePair<string, object>>.Empty, null);

        // Assert
        _fixture.MockAggregator.Received().Distribution(
            instrument.Name,
            measurement,
            MeasurementUnit.Custom(instrument.Unit!),
            Arg.Any<ImmutableDictionary<string, string>>()
        );
    }

    [Fact]
    public void RecordMeasurement_ObservableGaugeInstrument_CallsGauge()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateObservableGauge<int>("test.gauge", () => new [] { new Measurement<int>(2) }, "unit");
        const int measurement = 2;

        // Act
        var sut = _fixture.GetSut();
        sut.RecordMeasurement(instrument, measurement, ReadOnlySpan<KeyValuePair<string, object>>.Empty, null);

        // Assert
        _fixture.MockAggregator.Received().Gauge(
            instrument.Name,
            measurement,
            MeasurementUnit.Custom(instrument.Unit!),
            Arg.Any<ImmutableDictionary<string, string>>()
        );
    }
}
#endif
