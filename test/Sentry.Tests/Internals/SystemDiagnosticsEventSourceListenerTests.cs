#if !__MOBILE__
using System.Diagnostics.Tracing;
using Sentry.PlatformAbstractions;

namespace Sentry.Tests.Internals;

public class SystemDiagnosticsEventSourceListenerTests
{
    private class Fixture
    {
        public readonly IMetricAggregator MockAggregator = Substitute.For<IMetricAggregator>();
        public readonly ExperimentalMetricsOptions MetricsOptions = new ();

        public SystemDiagnosticsEventSourceListener GetSut() =>
            new(MetricsOptions, () => MockAggregator);
    }

    private readonly Fixture _fixture = new();

    [EventSource(Name = nameof(Source1))]
    private sealed class Source1 : EventSource
    {
        [Event(1, Message = "Message1", Level = EventLevel.LogAlways)]
        public void One()
        {
            WriteEvent(1);
        }
    }

    [SkippableFact]
    public void OnEventWritten_Counter_AggregatesCorrectly()
    {
        Skip.If(RuntimeInfo.GetRuntime().IsMono());

        // Arrange
        _fixture.MetricsOptions.CaptureSystemDiagnosticsEventSourceNames.Add(nameof(Source1));
        var total = 0;
        var counterName = nameof(Source1.One);
        _fixture.MockAggregator.Increment(
            counterName,
            Arg.Do<double>(_ => total++),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
            );

        // Act
        var sut = _fixture.GetSut();

        var testSource = new Source1();
        testSource.One();
        testSource.One();
        testSource.One();

        // Assert
        _fixture.MockAggregator.Received(3).Increment(
            counterName,
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
        );
        total.Should().Be(3);
    }

    [EventSource(Name = Source3.EventSourceName)]
    public sealed class Source3 : EventSource
    {
        public const string EventSourceName = nameof(Source3);

        [Event(3, Message = "Message3", Level = EventLevel.LogAlways)]
        public void Three()
        {
            WriteEvent(3);
        }
    }

    [EventSource(Name = Source7.EventSourceName)]
    public sealed class Source7 : EventSource
    {
        public const string EventSourceName = nameof(Source7);

        [Event(7, Message = "Message7", Level = EventLevel.LogAlways)]
        public void Seven()
        {
            WriteEvent(7);
        }
    }

    [SkippableFact]
    public void SystemDiagnosticsEventSourceListener_OnlyListensToMatchingCounter()
    {
        Skip.If(RuntimeInfo.GetRuntime().IsMono());

        // Arrange
        _fixture.MetricsOptions.CaptureSystemDiagnosticsEventSourceNames.Add(nameof(Source3));
        var total3 = 0;
        var counter3 = nameof(Source3.Three);
        _fixture.MockAggregator.Increment(
            counter3,
            Arg.Do<double>(_ => total3++),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
        );

        var total7 = 0;
        var counter7 = nameof(Source7.Seven);
        _fixture.MockAggregator.Increment(
            counter7,
            Arg.Do<double>(_ => total7++),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
        );

        // Act
        var sut = _fixture.GetSut();

        var source3 = new Source3();
        source3.Three();
        source3.Three();
        source3.Three();

        var source7 = new Source7();
        source7.Seven();
        source7.Seven();

        // Assert
        _fixture.MockAggregator.Received(3).Increment(
            counter3,
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
        );
        _fixture.MockAggregator.Received(0).Increment(
            counter7,
            Arg.Any<double>(),
            Arg.Any<MeasurementUnit>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<DateTimeOffset>()
        );
        total3.Should().Be(3);
        total7.Should().Be(0);
    }
}
#endif
