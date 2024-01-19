#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace Sentry.Tests.Internals;

public class SystemDiagnosticsMetricsListenerTests
{
    private class Fixture
    {
        public readonly IMetricAggregator MockAggregator = Substitute.For<IMetricAggregator>();
        public readonly ExperimentalMetricsOptions MetricsOptions = new ();

        public SystemDiagnosticsMetricsListener GetSut()
        {
            return new SystemDiagnosticsMetricsListener(MetricsOptions, () => MockAggregator);
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
    public void RecordMeasurement_UpDownCounterInstrument_CallsIncrement()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var instrument = testMeter.CreateUpDownCounter<int>("test.updowncounter", "unit");
        const int measurement = -2;
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
        var instrument = testMeter.CreateObservableGauge<int>("test.observable.gauge", () => new [] { new Measurement<int>(2) }, "unit");
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
        _fixture.MetricsOptions.CaptureSystemDiagnosticsInstruments.Add(instrument.Name);
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
        var instrument = testMeter.CreateObservableCounter("test.observable.counter",
            () => observedValues);
        _fixture.MetricsOptions.CaptureSystemDiagnosticsInstruments.Add(instrument.Name);
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

    [Fact]
    public void SystemDiagnosticsMetricsListener_ObservableUpDownCounter_AggregatesCorrectly()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        List<Measurement<int>> observedValues = [ new Measurement<int>(12), new Measurement<int>(-5) ];
        var instrument = testMeter.CreateObservableUpDownCounter("test.observable.updowncounter",
            () => observedValues);
        _fixture.MetricsOptions.CaptureSystemDiagnosticsInstruments.Add(instrument.Name);
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
        total.Should().Be(7);
    }

    [Fact]
    public void SystemDiagnosticsMetricsListener_OnlyListensToMatchingInstruments()
    {
        // Arrange
        var testMeter = new Meter("TestMeter", "1.0.0");
        var match = testMeter.CreateCounter<int>("OnlyListensToMatchingInstruments.match");
        var noMatch = testMeter.CreateCounter<int>("OnlyListensToMatchingInstruments.noMatch");
        _fixture.MetricsOptions.CaptureSystemDiagnosticsInstruments.Add(match.Name);
        var total = 0d;
        _fixture.MockAggregator.Increment(
            Arg.Any<string>(),
            Arg.Do<double>(x => total += x),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>());

        // Act
        var sut = _fixture.GetSut();
        match.Add(5);
        noMatch.Add(3);

        // Assert
        _fixture.MockAggregator.Received(1).Increment(
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>()
        );
        total.Should().Be(5);
    }

    [Fact]
    public void SystemDiagnosticsMetricsListener_OnlyListensToMatchingMeters()
    {
        // Arrange
        var matchingMeter = new Meter("OnlyListensToMatchingMeters.Matching", "1.0.0");
        var matching1 = matchingMeter.CreateCounter<int>("OnlyListensToMatchingInstruments.matching1");
        var matching2 = matchingMeter.CreateCounter<int>("OnlyListensToMatchingInstruments.matching2");
        var nonMatchingMeter = new Meter("OnlyListensToMatchingMeters.NonMatching", "1.0.0");
        var nonMatching1 = nonMatchingMeter.CreateCounter<int>("OnlyListensToMatchingInstruments.nonMatching1");
        _fixture.MetricsOptions.CaptureSystemDiagnosticsMeters.Add(matchingMeter.Name);
        var total = 0d;
        _fixture.MockAggregator.Increment(
            Arg.Any<string>(),
            Arg.Do<double>(x => total += x),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>());

        // Act
        var sut = _fixture.GetSut();
        matching1.Add(3);
        matching2.Add(5);
        nonMatching1.Add(7);

        // Assert
        _fixture.MockAggregator.Received(2).Increment(
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<ImmutableDictionary<string, string>>()
        );
        total.Should().Be(8);
    }
}
#endif
