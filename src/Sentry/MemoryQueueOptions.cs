using System;
using Sentry.Extensibility;

namespace Sentry
{
    public class BackgroundWorkerOptions
    {
        private int _maxQueueItems = 30;
        public int MaxQueueItems
        {
            get => _maxQueueItems;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "At least 1 item must be allowed in the queue.");
                }
                _maxQueueItems = value;
            }
        }

        // The time to keep running, in case there are requests queued up, after cancellation is requested
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(2);

        internal Func<SentryOptions, IBackgroundWorker> BackgroundWorkerFactory { get; set; }
    }
}
