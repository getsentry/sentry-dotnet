using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

using Xunit;
using NSubstitute;

public class BlockingMonitorTests
{
    [Fact]
    public void BlockingStart_ThreadNotFromThreadPool_NoAction()
    {
        // Arrange
        var getHub = Substitute.For<Func<IHub>>();
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var thread = new Thread(() =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);

            // Assert
            getHub.DidNotReceive().Invoke();
        });
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void BlockingEnd_ThreadNotFromThreadPool_NoAction()
    {
        // Arrange
        var getHub = Substitute.For<Func<IHub>>();
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var thread = new Thread(() =>
        {
            monitor.BlockingEnd();
            BlockingMonitor.t_recursionCount.Should().Be(0);
        });
        thread.Start();
        thread.Join();
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
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);

            // Assert
            getHub.Received(1).Invoke();
            hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());

            resetEvent.Set();
        });
        resetEvent.WaitOne();
    }


    [Fact]
    public void BlockingStart_Twice_CapturesEventOnce()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var getHub = Substitute.For<Func<IHub>>();
        getHub.Invoke().Returns(hub);
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);
            monitor.BlockingStart(DetectionSource.SynchronizationContext);

            // Assert
            getHub.Received(1).Invoke();
            hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());

            resetEvent.Set();
        });
        resetEvent.WaitOne();
    }

    [Fact]
    public void BlockingStartEnd_CorrectRecursionCount()
    {
        // Arrange
        var monitor = new BlockingMonitor(Substitute.For<Func<IHub>>(), new SentryOptions());

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);
            monitor.BlockingEnd();

            // Assert
            BlockingMonitor.t_recursionCount.Should().Be(0);

            resetEvent.Set();
        });
        resetEvent.WaitOne();
    }

    [Fact]
    public void BlockingStartEnd_SameCall_OnlyCaptureOnce()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var getHub = Substitute.For<Func<IHub>>();
        getHub.Invoke().Returns(hub);
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            for (int i = 0; i < 2; i++)
            {
                monitor.BlockingStart(DetectionSource.SynchronizationContext);
                monitor.BlockingEnd();
            }

            // Assert
            getHub.Received(1).Invoke();
            hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());

            resetEvent.Set();
        });
        resetEvent.WaitOne();
    }

    [Fact]
    public void BlockingStartEnd_DifferentCalls_CaptureSeparately()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var getHub = Substitute.For<Func<IHub>>();
        getHub.Invoke().Returns(hub);
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);
            monitor.BlockingEnd();

            monitor.BlockingStart(DetectionSource.SynchronizationContext);
            monitor.BlockingEnd();

            // Assert
            getHub.Received(2).Invoke();
            hub.Received(2).CaptureEvent(Arg.Any<SentryEvent>());

            resetEvent.Set();
        });
        resetEvent.WaitOne();
    }
}
