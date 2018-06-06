using System;

namespace Sentry
{
    public class BackgroundWorkerOptions
    {
        private int _maxQueueItems;
        public int MaxQueueItems
        {
            get
            {
                if (_maxQueueItems < 1)
                {
                    _maxQueueItems = 30; // Default
                }
                return _maxQueueItems;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "At least 1 item must be allowed in the queue.");
                }
                _maxQueueItems = value;
            }
        }

        public TimeSpan FullQueueBlockTimeout = TimeSpan.Zero;
        public TimeSpan EmptyQueueDelay = TimeSpan.FromSeconds(1);
        // The time to keep running, in case there are requests queued up, after cancellation is requested
        public TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);
    }
}
