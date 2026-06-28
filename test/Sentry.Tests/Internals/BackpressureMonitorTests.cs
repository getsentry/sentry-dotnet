namespace Sentry.Tests.Internals;

public class BackpressureMonitorTests
{
    private class Fixture
    {
        private IDiagnosticLogger Logger { get; } = Substitute.For<IDiagnosticLogger>();
        public ISystemClock Clock { get; } = Substitute.For<ISystemClock>();
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public BackpressureMonitor GetSut() => new(Logger, Clock, enablePeriodicHealthCheck: false);
    }

    private readonly Fixture _fixture = new();

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
    [InlineData(10, 1.0 / 1024)]
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
    public void DoHealthCheck_Unhealthy_DownsampleLevelIncreases()
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
    public void DoHealthCheck_Unhealthy_MaximumDownsampleLevelRespected()
    {
        // Arrange
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now);
        using var monitor = _fixture.GetSut();
        monitor.RecordQueueOverflow();

        // Act
        var overmax = BackpressureMonitor.MaxDownsamples + 1;
        for (var i = 0; i <= overmax; i++)
        {
            monitor.DoHealthCheck();
        }

        // Assert
        monitor.DownsampleLevel.Should().Be(BackpressureMonitor.MaxDownsamples);
    }

    [Fact]
    public void DoHealthCheck_Healthy_DownsampleLevelResets()
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

    [Fact]
    public async Task Dispose_DoesNotBlockOnWorkerTask()
    {
        // Arrange
        // Run the periodic worker on a scheduler we never pump, so the worker task never completes. This models
        // a single-threaded runtime (e.g. Unity WebGL) where the only thread able to run the worker's
        // cancellation continuation is the one calling Dispose. A blocking _workerTask.Wait() would deadlock
        // there; Dispose must instead just cancel and return. See https://github.com/getsentry/sentry-dotnet/issues/5237
        var scheduler = new NeverRunsTaskScheduler();
        var monitor = new BackpressureMonitor(null, _fixture.Clock, enablePeriodicHealthCheck: true, scheduler: scheduler);

        // Act
        var disposed = Task.Run(() => monitor.Dispose());

        // Assert
        var completed = await Task.WhenAny(disposed, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().BeSameAs(disposed, "Dispose must not block waiting on the worker task to complete");
        await disposed; // surface any exception thrown by Dispose
    }

    /// <summary>
    /// A scheduler that queues tasks but never executes them - lets us hold the worker task in a state that
    /// never completes, so a Dispose that blocks on it would hang.
    /// </summary>
    private sealed class NeverRunsTaskScheduler : TaskScheduler
    {
        private readonly List<Task> _tasks = new();
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            lock (_tasks)
            {
                return _tasks.ToArray();
            }
        }

        protected override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.Add(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    }
}
