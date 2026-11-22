using System.Diagnostics.Tracing;
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

#if !__MOBILE__ // The TPL event source emits nothing on Android and iOS
    [Fact]
    public async Task Enable_BlockingCall_BlockingStart()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        using var listener = new TaskBlockingListener(monitor);

        listener.Enable();
        await Task.Run(() => Task.Delay(20).Wait());

        monitor.Received().BlockingStart(DetectionSource.EventListener);
    }

    [Fact]
    public async Task Enable_TplEventSourceCreatedAfterwards_BlockingStart()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        using var listener = new LateTplEventSourceListener(monitor);

        listener.Enable();
        listener.RecreateTplEventSource();
        await Task.Run(() => Task.Delay(20).Wait());

        monitor.Received().BlockingStart(DetectionSource.EventListener);
    }

    private sealed class LateTplEventSourceListener(IBlockingMonitor monitor) : TaskBlockingListener(monitor)
    {
        public void RecreateTplEventSource()
        {
            var tplEventSource = EventSource.GetSources().Single(source => source.Guid == s_tplGuid);
            DisableEvents(tplEventSource);
            OnEventSourceCreated(tplEventSource);
        }
    }
#endif

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
