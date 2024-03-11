using System.Diagnostics.Tracing;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    // Tips of the Toub
    internal class TaskBlockingListener : EventListener
    {
        // https://github.com/dotnet/runtime/blob/94f212275b2f51ca67025d677d7d5c5bc75f670f/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/TplEventSource.cs#L13
        internal static readonly Guid s_tplGuid = new Guid("2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5");

        private readonly IBlockingMonitor _monitor;
        private readonly ITaskBlockingListenerState _state;

        private static Lazy<StaticTaskBlockingListenerState> LazyDefaultState => new();
        internal static StaticTaskBlockingListenerState DefaultState => LazyDefaultState.Value;

        public TaskBlockingListener(IBlockingMonitor monitor)
            : this(monitor, null)
        {
        }

        /// <summary>
        /// For testing only
        /// </summary>
        internal TaskBlockingListener(IBlockingMonitor monitor, ITaskBlockingListenerState? state)
        {
            _monitor = monitor;
            _state = state ?? DefaultState;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Guid == s_tplGuid)
            {
                // 3 == Task|TaskTransfer
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)3);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                return;
            }

            DoHandleEvent(eventData.EventId, eventData.Payload);
        }

        internal void DoHandleEvent(int eventId, ReadOnlyCollection<object?>? payload)
        {
            var monitor = _state.IsSuppressed() ? null : _monitor;

            if (eventId == 10 && // TASKWAITBEGIN_ID
                payload != null &&
                payload.Count > 3 &&
                payload[3] is int value && // Behavior
                value == 1) // TaskWaitBehavior.Synchronous
            {
                monitor?.BlockingStart(DetectionSource.EventListener);
            }
            else if (eventId == 11) // TASKWAITEND_ID
            {
                monitor?.BlockingEnd();
            }
        }
    }
}
