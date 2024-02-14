using System.Diagnostics.Tracing;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    // Tips of the Toub
    internal sealed class TaskBlockingListener : EventListener
    {
        // https://github.com/dotnet/runtime/blob/94f212275b2f51ca67025d677d7d5c5bc75f670f/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/TplEventSource.cs#L13
        private static readonly Guid s_tplGuid = new Guid("2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5");

        [ThreadStatic] private static int t_suppressionCount;

        [ThreadStatic] private static int t_recursionCount;

        private readonly BlockingMonitor _monitor;

        public TaskBlockingListener(BlockingMonitor monitor) => _monitor = monitor;

        internal static void Suppress() => t_suppressionCount++;
        internal static void Restore() => t_suppressionCount--;

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

            IBlockingMonitor monitor = t_suppressionCount > 0 ? DisabledBlockingMonitor.Instance : _monitor;

            if (eventData.EventId == 10 && // TASKWAITBEGIN_ID
                eventData.Payload != null &&
                eventData.Payload.Count > 3 &&
                eventData.Payload[3] is int value && // Behavior
                value == 1) // TaskWaitBehavior.Synchronous
            {
                t_recursionCount++;
                monitor.BlockingStart(DetectionSource.EventListener);
            }
            else if (eventData.EventId == 11 // TASKWAITEND_ID
                     && t_recursionCount > 0)
            {
                t_recursionCount--;
                monitor.BlockingEnd();
            }
        }
    }
}
