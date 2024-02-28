// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting

namespace Sentry.Ben.BlockingDetector
{
    // Tips of the Toub
    internal sealed class DetectBlockingSynchronizationContext : SynchronizationContext
    {
        private readonly IBlockingMonitor _monitor;
        private readonly SynchronizationContext? _syncCtx;

        internal int _isSuppressed;

        internal void Suppress() => Interlocked.Exchange(ref _isSuppressed, _isSuppressed + 1);
        internal void Restore() => Interlocked.Exchange(ref _isSuppressed, _isSuppressed - 1);

        public DetectBlockingSynchronizationContext(IBlockingMonitor monitor)
        {
            _monitor = monitor;

            SetWaitNotificationRequired();
        }

        public DetectBlockingSynchronizationContext(IBlockingMonitor monitor, SynchronizationContext? syncCtx)
            : this(monitor)
            => _syncCtx = syncCtx;

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (millisecondsTimeout == 0)
            {
                return WaitInternal(waitHandles, waitAll, millisecondsTimeout);
            }

            var monitor = _isSuppressed > 0 ? null : _monitor;

            monitor?.BlockingStart(DetectionSource.SynchronizationContext);

            try
            {
                return WaitInternal(waitHandles, waitAll, millisecondsTimeout);
            }
            finally
            {
                monitor?.BlockingEnd();
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
