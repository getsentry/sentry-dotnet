using Sentry.Protocol.Metrics;

namespace Sentry.Tests;

public class TimingTests
{
    private class Fixture
    {
        public readonly IHub Hub;
        public IMetricHub MetricHub { get; }
        public SentryOptions Options { get; }
        public IDiagnosticLogger Logger { get; }
        public MetricAggregator MetricAggregator { get; }

        public string Key { get; set; } = "key";

        public MeasurementUnit.Duration Unit { get; set; } = MeasurementUnit.Duration.Second;
        public Dictionary<string, string> Tags { get; set; } = new();

        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            MetricHub = Substitute.For<IMetricHub>();
            Logger = Substitute.For<IDiagnosticLogger>();
            Logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Options = new()
            {
                Debug = true,
                DiagnosticLogger = Logger
            };
            MetricAggregator = Substitute.For<MetricAggregator>(Options, MetricHub, null, true);
        }

        public Timing GetSut() => new(MetricAggregator, Options, Key, Unit, Tags, 1);
    }
    private readonly Fixture _fixture = new();

    [Fact]
    public void Constructor_RecordsCodeLocation()
    {
        // Act
        var timing = _fixture.GetSut();

        // Assert
        _fixture.MetricAggregator.Received(1).RecordCodeLocation(MetricType.Distribution, _fixture.Key, MeasurementUnit.Duration.Second, 2, timing._startTime);
    }

    [Fact]
    public void Constructor_StartsStopwatch()
    {
        // Act
        var timing = _fixture.GetSut();

        // Assert
        timing._stopwatch.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Dispose_StopsStopwatch()
    {
        // Arrange
        var timing = _fixture.GetSut();

        // Act
        var stopwatch = timing._stopwatch;
        timing.Dispose();

        // Assert
        stopwatch.IsRunning.Should().BeFalse();
    }

    [Theory]
    [InlineData(MeasurementUnit.Duration.Week, 7)]
    [InlineData(MeasurementUnit.Duration.Day, 1)]
    [InlineData(MeasurementUnit.Duration.Hour, 1 / 24.0)]
    [InlineData(MeasurementUnit.Duration.Minute, 1 / (24.0 * 60))]
    [InlineData(MeasurementUnit.Duration.Second, 1 / (24.0 * 60 * 60))]
    [InlineData(MeasurementUnit.Duration.Millisecond, 1 / (24.0 * 60 * 60 * 1000))]
    [InlineData(MeasurementUnit.Duration.Microsecond, 1 / (24.0 * 60 * 60 * 1000000))]
    [InlineData(MeasurementUnit.Duration.Nanosecond, 1 / (24.0 * 60 * 60 * 1000000000))]
    public void DisposeInternal_ValidUnits_RecordsTiming(MeasurementUnit.Duration unit, double expectedRatio)
    {
        // Arrange
        _fixture.Unit = unit;
        var timing = _fixture.GetSut();
        var elapsed = TimeSpan.FromDays(1); // 1 day

        // Act
        timing.DisposeInternal(elapsed);

        // Assert
        _fixture.MetricAggregator.Received(1).Timing(
            Arg.Any<string>(),
            elapsed.TotalDays / expectedRatio, // Expected value
            unit,
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void Dispose_InvalidUnit_LogsError()
    {
        // Arrange
        _fixture.Unit = (MeasurementUnit.Duration)int.MaxValue;
        var timing = _fixture.GetSut();

        // Act
        timing.Dispose();

        // Assert
        _fixture.MetricAggregator.Received(0).Timing(
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit.Duration>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
        );
        _fixture.Logger.Received(1).Log(
            SentryLevel.Error,
            "Error capturing timing '{0}'",
            Arg.Any<ArgumentOutOfRangeException>(),
            _fixture.Key
            );
    }
}
