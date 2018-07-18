using System;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Configuration to the worker which sends queued events to Sentry
    /// </summary>
    public class BackgroundWorkerOptions
    {
        private int _maxQueueItems = 30;

        /// <summary>
        /// The maximum number of events to keep while the worker attempts to send them
        /// </summary>
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

        /// <summary>
        /// How long to wait for events to be sent before shutdown
        /// </summary>
        /// <remarks>
        /// In case there are events queued when the SDK is closed, upper bound limit to wait
        /// for the worker to send the events to Sentry.
        /// </remarks>
        /// <example>
        /// The SDK is closed while the queue has 1 event queued.
        /// The worker takes 50 milliseconds to send an event to Sentry.
        /// Even though default settings say 2 seconds, closing the SDK would block for 50ms.
        /// </example>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(2);

        internal IBackgroundWorker BackgroundWorker { get; set; }
    }
}
