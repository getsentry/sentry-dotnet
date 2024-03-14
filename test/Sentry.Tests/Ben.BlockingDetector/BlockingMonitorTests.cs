using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

using NSubstitute;
using Xunit;

public class BlockingMonitorTests
{
    [Fact]
    public void BlockingMonitor_DefaultConstructor_StaticRecursionTracker()
    {
        // Arrange
        var getHub = Substitute.For<Func<IHub>>();
        var options = new SentryOptions();
        var monitor = new BlockingMonitor(getHub, options);

        // Assert
        Assert.IsType<StaticRecursionTracker>(monitor._recursionTracker);
    }

    [Fact]
    public void BlockingStart_ThreadNotFromThreadPool_NoAction()
    {
        // Arrange
        var getHub = Substitute.For<Func<IHub>>();
        var options = new SentryOptions();
        var recursionTracker = Substitute.For<IRecursionTracker>();
        var monitor = new BlockingMonitor(getHub, options, recursionTracker);

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
        var recursionTracker = Substitute.For<IRecursionTracker>();
        var monitor = new BlockingMonitor(getHub, options, recursionTracker);

        // Act
        var thread = new Thread(() =>
        {
            monitor.BlockingEnd();
            recursionTracker.DidNotReceive().Recurse();
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
        var recursionTracker = Substitute.For<IRecursionTracker>();
        recursionTracker.IsFirstRecursion().Returns(true);
        var monitor = new BlockingMonitor(getHub, options, recursionTracker);

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);

            // Assert
            recursionTracker.Received(1).Recurse();
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
        var recursionTracker = Substitute.For<IRecursionTracker>();
        var monitor = new BlockingMonitor(getHub, options, recursionTracker);

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            recursionTracker.IsFirstRecursion().Returns(true);
            monitor.BlockingStart(DetectionSource.SynchronizationContext);
            recursionTracker.IsFirstRecursion().Returns(false);
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
        var recursionTracker = Substitute.For<IRecursionTracker>();
        var monitor = new BlockingMonitor(Substitute.For<Func<IHub>>(), new SentryOptions(), recursionTracker);

        // Act
        var resetEvent = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            monitor.BlockingStart(DetectionSource.SynchronizationContext);
            monitor.BlockingEnd();

            // Assert
            recursionTracker.Received(1).Recurse();
            recursionTracker.Received(1).Backtrack();

            resetEvent.Set();
        });
        resetEvent.WaitOne();
    }
}
