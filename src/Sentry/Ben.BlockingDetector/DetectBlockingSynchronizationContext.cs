// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.Diagnostics
{
    // Tips of the Toub
    internal sealed class DetectBlockingSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingMonitor _monitor;
        private readonly SynchronizationContext? _syncCtx;

        public DetectBlockingSynchronizationContext(BlockingMonitor monitor)
        {
            _monitor = monitor;

            SetWaitNotificationRequired();
        }

        public DetectBlockingSynchronizationContext(BlockingMonitor monitor, SynchronizationContext? syncCtx)
            : this(monitor)
            => _syncCtx = syncCtx;

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (millisecondsTimeout == 0)
            {
                return WaitInternal(waitHandles, waitAll, millisecondsTimeout);
            }

            _monitor.BlockingStart(DetectionSource.SynchronizationContext);

            try
            {
                return WaitInternal(waitHandles, waitAll, millisecondsTimeout);
            }
            finally
            {
                _monitor.BlockingEnd();
            }
        }

        private int WaitInternal(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (_syncCtx != null)
            {
                return _syncCtx.Wait(waitHandles, waitAll, millisecondsTimeout);
            }

            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }
    }
}
