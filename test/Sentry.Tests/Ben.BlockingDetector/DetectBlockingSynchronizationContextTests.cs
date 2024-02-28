using Sentry.Ben.BlockingDetector;

namespace Sentry.Tests.Ben.BlockingDetector;

public class DetectBlockingSynchronizationContextTests
{
    [Fact]
    public void SuppressAndRestoreTests()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var context = new DetectBlockingSynchronizationContext(monitor);

        context.Suppress();
        Assert.Equal(1, context._isSuppressed);
        context.Restore();
        Assert.Equal(0, context._isSuppressed);
    }

    [Fact]
    public void Wait_ZeroTimeout_SkipsBlockingDetection()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var received = 0;
        var fakeSyncContext = new FakeSyncContext((_, _, _) =>
        {
            received++;
            return 0;
        });

        var context = new DetectBlockingSynchronizationContext(monitor, fakeSyncContext);

        var handles = new IntPtr[1];
        var result = context.Wait(handles, false, 0);

        monitor.Received(0).BlockingStart(DetectionSource.SynchronizationContext);
        monitor.Received(0).BlockingEnd();
        received.Should().Be(1);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Wait_NonZeroTimeout_InvokesBlockingDetection()
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var received = 0;
        var fakeSyncContext = new FakeSyncContext((_, _, _) =>
        {
            received++;
            return 0;
        });
        var context = new DetectBlockingSynchronizationContext(monitor, fakeSyncContext);

        var handles = new IntPtr[1];
        var result = context.Wait(handles, false, 1);

        monitor.Received(1).BlockingStart(DetectionSource.SynchronizationContext);
        received.Should().Be(1);
        monitor.Received(1).BlockingEnd();
        Assert.NotEqual(-1, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void Wait_WithSyncContext_CallsInternalWait(int millisecondsTimeout)
    {
        var monitor = Substitute.For<IBlockingMonitor>();
        var received = 0;
        var fakeSyncContext = new FakeSyncContext((_, _, _) =>
        {
            received++;
            return 0;
        });

        var context = new DetectBlockingSynchronizationContext(monitor, fakeSyncContext);

        var handles = new IntPtr[1];
        var result = context.Wait(handles, false, millisecondsTimeout);

        received.Should().Be(1);
        Assert.Equal(0, result);
    }

    class FakeSyncContext : SynchronizationContext
    {
        private readonly Func<IntPtr[], bool, int, int> _waitCallback;

        public FakeSyncContext(Func<IntPtr[], bool, int, int> waitCallback)
        {
            _waitCallback = waitCallback;
        }

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return _waitCallback(waitHandles, waitAll, millisecondsTimeout);
        }
    }
}
