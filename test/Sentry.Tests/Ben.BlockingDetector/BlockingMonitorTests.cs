using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

using Xunit;
using NSubstitute;

public class BlockingMonitorTests
{
    public BlockingMonitorTests()
    {
        // Reset ThreadStatic
        BlockingMonitor.t_recursionCount = 0;
    }

    [Fact]
    public void BlockingStart_ThreadNotFromThreadPool_NoAction()
    {
        // Arrange
        var getHub = Substitute.For<Func<IHub>>();
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var thread = new Thread(() => monitor.BlockingStart(DetectionSource.SynchronizationContext));
        thread.Start();
        thread.Join();

        // Assert
        BlockingMonitor.t_recursionCount.Should().Be(0);
        getHub.DidNotReceive().Invoke();
    }

    [Fact]
    public void BlockingStart_ThreadFromThreadPool_CapturesEvent()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var getHub = Substitute.For<Func<IHub>>();
        getHub.Invoke().Returns(hub);
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        monitor.BlockingStart(DetectionSource.SynchronizationContext);

        // Assert
        BlockingMonitor.t_recursionCount.Should().Be(1);
        getHub.Received(1).Invoke();
        hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void BlockingEnd_ThreadNotFromThreadPool_NoAction()
    {
        // Arrange
        var getHub = Substitute.For<Func<IHub>>();
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var thread = new Thread(() => monitor.BlockingEnd());
        thread.Start();
        thread.Join();

        // Assert
        BlockingMonitor.t_recursionCount.Should().Be(0);
    }

    [Fact]
    public void BlockingStartEnd_CorrectRecursionCount()
    {
        // Arrange
        var monitor = new BlockingMonitor(Substitute.For<Func<IHub>>(), new SentryOptions()
        {
            Debug = true,
            DiagnosticLevel = SentryLevel.Debug
        });

        // Act
        monitor.BlockingStart(DetectionSource.SynchronizationContext);
        monitor.BlockingEnd();

        // Assert
        BlockingMonitor.t_recursionCount.Should().Be(0);
    }
}
