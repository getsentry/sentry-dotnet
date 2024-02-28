using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

public class TaskBlockingListenerTests
{
    [Fact]
    public void DoHandleEvent_OnTaskWaitBeginNotSuppressed_BlockingStart()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var state = Substitute.For<ITaskBlockingListenerState>();
        state.IsSuppressed().Returns(false);
        var listener = new TaskBlockingListener(monitor, state);

        listener.DoHandleEvent(10, new List<object> {0, 0, 0, 1}.AsReadOnly());

        state.Received(1).Recurse();
        monitor.Received(1).BlockingStart(DetectionSource.EventListener);
    }

    [Fact]
    public void DoHandleEvent_SuppressedOnTaskWaitBegin_BlockingSkipped()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var state = Substitute.For<ITaskBlockingListenerState>();
        state.IsSuppressed().Returns(true);
        var listener = new TaskBlockingListener(monitor, state);

        listener.DoHandleEvent(10, new List<object> {0, 0, 0, 1}.AsReadOnly());

        monitor.DidNotReceive().BlockingStart(Arg.Any<DetectionSource>());
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void DoHandleEvent_IsRecursiveOnTaskWaitEnd_BlockingEnd(bool isRecursive, int expectedReceived)
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var state = Substitute.For<ITaskBlockingListenerState>();
        state.IsRecursive().Returns(isRecursive);
        var listener = new TaskBlockingListener(monitor, state);

        listener.DoHandleEvent(11, new List<object>().AsReadOnly());

        state.Received(expectedReceived).Backtrack();
        monitor.Received(expectedReceived).BlockingEnd();
    }
}
