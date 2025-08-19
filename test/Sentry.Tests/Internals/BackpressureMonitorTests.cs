namespace Sentry.Tests.Internals;

public class BackpressureMonitorTests
{
    private class Fixture
    {
        private IDiagnosticLogger Logger { get; } = Substitute.For<IDiagnosticLogger>();
        public ISystemClock Clock { get; } = Substitute.For<ISystemClock>();
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public BackpressureMonitor GetSut() => new (Logger, Clock, startImmediately: false);
    }

    private readonly Fixture _fixture = new ();

    [Fact]
    public void DownsampleFactor_Initial_IsOne()
    {
        // Arrange
        using var monitor = _fixture.GetSut();

        // Act
        var factor = monitor.DownsampleFactor;

        // Assert
        factor.Should().Be(1.0);
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1, 0.5)]
    [InlineData(2, 0.25)]
    [InlineData(10, 1.0/1024)]
    public void DownsampleFactor_CalculatesCorrectly(int level, double expected)
    {
        // Arrange
        using var monitor = _fixture.GetSut();
        monitor.SetDownsampleLevel(level);

        // Act
        var factor = monitor.DownsampleFactor;

        // Assert
        factor.Should().BeApproximately(expected, 1e-8);
    }

    [Fact]
    public void RecordRateLimitHit_UpdatesState()
    {
        // Arrange
        using var monitor = _fixture.GetSut();
        var when = _fixture.Now.Subtract(TimeSpan.FromSeconds(1));

        // Act
        monitor.RecordRateLimitHit(when);

        // Assert
        monitor.LastRateLimitEventTicks.Should().Be(when.Ticks);
    }

    [Fact]
    public void RecordQueueOverflow_UpdatesState()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();

        // Act
        monitor.RecordQueueOverflow();

        // Assert
        monitor.LastQueueOverflowTicks.Should().Be(_fixture.Now.Ticks);
    }

    [Fact]
    public void IsHealthy_True_WhenNoRecentEvents()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();

        // Act & Assert
        monitor.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public void IsHealthy_False_WhenRecentQueueOverflow()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();

        // Act
        monitor.RecordQueueOverflow();

        // Assert
        monitor.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void IsHealthy_False_WhenRecentRateLimit()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();

        // Act
        monitor.RecordRateLimitHit(_fixture.Now);

        // Assert
        monitor.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void DownsampleLevel_Increases_WhenUnhealthy()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();
        monitor.RecordQueueOverflow();

        // Act
        monitor.DoHealthCheck();

        // Assert
        monitor.DownsampleLevel.Should().Be(1);
    }

    [Fact]
    public void DownsampleLevel_Resets_WhenHealthy()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();
        monitor.SetDownsampleLevel(2);

        // Act
        monitor.DoHealthCheck();

        // Assert
        monitor.IsHealthy.Should().BeTrue();
        monitor.DownsampleLevel.Should().Be(0);
    }
}
