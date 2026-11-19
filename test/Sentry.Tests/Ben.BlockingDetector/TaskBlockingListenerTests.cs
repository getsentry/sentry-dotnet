using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

public class TaskBlockingListenerTests
{
    [Fact]
    public async Task Constructor_NotEnabled_IgnoresBlockingCalls()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        using var listener = new TaskBlockingListener(monitor);

        await Task.Run(() => Task.Delay(20).Wait());

        monitor.DidNotReceiveWithAnyArgs().BlockingStart(default);
    }

    [Fact]
    public async Task Enable_BlockingCall_BlockingStart()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        using var listener = new TaskBlockingListener(monitor);

        listener.Enable();
        await Task.Run(() => Task.Delay(20).Wait());

        monitor.Received().BlockingStart(DetectionSource.EventListener);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void DoHandleEvent_OnTaskWaitBegin_BlockingStart(bool isSuppressed, int expectedReceived)
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var state = Substitute.For<ITaskBlockingListenerState>();
        state.IsSuppressed().Returns(isSuppressed);
        var listener = new TaskBlockingListener(monitor, state);

        listener.DoHandleEvent(10, new List<object> { 0, 0, 0, 1 }.AsReadOnly());

        monitor.Received(expectedReceived).BlockingStart(DetectionSource.EventListener);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void DoHandleEvent_OnTaskWaitEnd_BlockingEnd(bool isSuppressed, int expectedReceived)
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var state = Substitute.For<ITaskBlockingListenerState>();
        state.IsSuppressed().Returns(isSuppressed);
        var listener = new TaskBlockingListener(monitor, state);

        listener.DoHandleEvent(11, new List<object>().AsReadOnly());

        monitor.Received(expectedReceived).BlockingEnd();
    }
}
