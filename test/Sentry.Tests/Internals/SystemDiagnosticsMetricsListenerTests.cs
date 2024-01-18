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
        ReadOnlySpan<KeyValuePair<string, object>> tags = [
            new KeyValuePair<string, object>("tag1", "value1"),
            new KeyValuePair<string, object>("tag2", 2),
        ];
        var expectedTags = tags.ToImmutableArray().ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );

        // Act
        var sut = _fixture.GetSut();
        sut.RecordMeasurement(instrument, measurement, tags, null);

        // Assert
        _fixture.MockAggregator.Received().Increment(
            instrument.Name,
            measurement,
            MeasurementUnit.Custom(instrument.Unit!),
            Arg.Is<ImmutableDictionary<string, string>>(arg =>
                    expectedTags.All(tag => arg.ContainsKey(tag.Key) && arg[tag.Key] == tag.Value)
                )
            );
    }

    [Fact]
    public void RecordMeasurement_HistogramInstrument_CallsDistribution()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateHistogram<int>("test.histogram", "unit");
        const int measurement = 2;
        ReadOnlySpan<KeyValuePair<string, object>> tags = [
            new KeyValuePair<string, object>("tag1", "value1"),
            new KeyValuePair<string, object>("tag2", 2),
        ];
        var expectedTags = tags.ToImmutableArray().ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );

        // Act
        var sut = _fixture.GetSut();
        sut.RecordMeasurement(instrument, measurement, tags, null);

        // Assert
        _fixture.MockAggregator.Received().Distribution(
            instrument.Name,
            measurement,
            MeasurementUnit.Custom(instrument.Unit!),
            Arg.Is<ImmutableDictionary<string, string>>(arg =>
                expectedTags.All(tag => arg.ContainsKey(tag.Key) && arg[tag.Key] == tag.Value)
            )
        );
    }

    [Fact]
    public void RecordMeasurement_ObservableGaugeInstrument_CallsGauge()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateObservableGauge<int>("test.gauge", () => new [] { new Measurement<int>(2) }, "unit");
        const int measurement = 2;
        ReadOnlySpan<KeyValuePair<string, object>> tags = [
            new KeyValuePair<string, object>("tag1", "value1"),
            new KeyValuePair<string, object>("tag2", 2),
        ];
        var expectedTags = tags.ToImmutableArray().ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );

        // Act
        var sut = _fixture.GetSut();
        sut.RecordMeasurement(instrument, measurement, tags, null);

        // Assert
        _fixture.MockAggregator.Received().Gauge(
            instrument.Name,
            measurement,
            MeasurementUnit.Custom(instrument.Unit!),
            Arg.Is<ImmutableDictionary<string, string>>(arg =>
                expectedTags.All(tag => arg.ContainsKey(tag.Key) && arg[tag.Key] == tag.Value)
            )
        );
    }

    [Fact]
    public void SystemDiagnosticsMetricsListener_Counter_AggregatesCorrectly()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateCounter<int>("test.counter", "unit");
        _fixture.CaptureInstruments.Add(instrument.Name);
        var total = 0d;
        _fixture.MockAggregator.Increment(
            instrument.Name,
            Arg.Do<double>(x => total += x),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>());

        // Act
        var sut = _fixture.GetSut();
        instrument.Add(2);
        instrument.Add(3);

        // Assert
        _fixture.MockAggregator.Received(2).Increment(
            instrument.Name,
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>()
        );
        total.Should().Be(5);
    }

    [Fact]
    public void SystemDiagnosticsMetricsListener_ObservableCounter_AggregatesCorrectly()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        List<Measurement<int>> observedValues = [ new Measurement<int>(2), new Measurement<int>(3) ];
        var instrument = testMeter.CreateObservableCounter<int>("test.counter",
            () => observedValues);
        _fixture.CaptureInstruments.Add(instrument.Name);
        var total = 0d;
        _fixture.MockAggregator.Increment(
            instrument.Name,
            Arg.Do<double>(x => total += x),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>());

        // Act
        var sut = _fixture.GetSut();
        sut._sentryListener.RecordObservableInstruments();

        // Assert
        _fixture.MockAggregator.Received(2).Increment(
            instrument.Name,
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>()
        );
        total.Should().Be(5);
    }
}
#endif
