using Sentry.Internal;
using Sentry.Protocol;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    internal interface IBlockingMonitor
    {
        void BlockingStart(DetectionSource detectionSource);
        void BlockingEnd();
    }

    internal class BlockingMonitor : IBlockingMonitor
    {
        private readonly Func<IHub> _getHub;
        private readonly SentryOptions _options;
        internal readonly IRecursionTracker _recursionTracker;

        public BlockingMonitor(Func<IHub> getHub, SentryOptions options)
            : this(getHub, options, new StaticRecursionTracker())
        {
        }

        internal BlockingMonitor(Func<IHub> getHub, SentryOptions options, IRecursionTracker recursionTracker)
        {
            _getHub = getHub;
            _options = options;
            _recursionTracker = recursionTracker;
        }

        private static bool ShouldSkipFrame(string? frameInfo) =>
            frameInfo?.StartsWith("Sentry.Ben") == true
            // Skip frames relating to the TaskBlockingListener
            || frameInfo?.StartsWith("System.Diagnostics") == true
            // Skip frames relating to the async state machine
            || frameInfo?.StartsWith("System.Threading") == true;

        public void BlockingStart(DetectionSource detectionSource)
        {
            // From Stephen Cleary:
            // "The default SynchronizationContext queues its asynchronous delegates to the ThreadPool but executes its
            // synchronous delegates directly on the calling thread."
            //
            // Implicitly then, if we're not on a ThreadPool thread, we're not in an async context.
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                return;
            }

            _recursionTracker.Recurse();

            try
            {
                if (!_recursionTracker.IsFirstRecursion())
                {
                    return;
                }

                var stackTrace = DebugStackTrace.Create(
                    _options,
                    new StackTrace(true),
                    true,
                    ShouldSkipFrame
                );
                var evt = new SentryEvent
                {
                    Level = SentryLevel.Warning,
                    Message =
                        "Blocking method has been invoked and blocked, this can lead to ThreadPool starvation. Learn more about it: " +
                        "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices#avoid-blocking-calls ",
                    SentryExceptions = new[]
                    {
                        new SentryException
                        {
                            ThreadId = Environment.CurrentManagedThreadId,
                            Mechanism = new Mechanism
                            {
                                Type = "BlockingCallDetector",
                                Handled = false,
                                Description = "Blocking calls can cause ThreadPool starvation."
                            },
                            Type = "Blocking call detected",
                            Stacktrace = stackTrace,
                        }
                    },
                };
                evt.SetTag("DetectionSource", detectionSource.ToString());

                _getHub().CaptureEvent(evt);
            }
            catch
            {
                // ignored
            }
        }

        public void BlockingEnd()
        {
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                return;
            }

            _recursionTracker.Backtrack();
        }
    }

    internal enum DetectionSource
    {
        SynchronizationContext,
        EventListener
    }
}
