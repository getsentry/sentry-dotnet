using Sentry.Internal;
using Sentry.Protocol;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    internal class BlockingMonitor
    {
        [ThreadStatic]
        internal static int RecursionCount;

        private readonly Func<IHub> _getHub;
        private readonly SentryOptions _options;

        public BlockingMonitor(Func<IHub> getHub, SentryOptions options)
        {
            _getHub = getHub;
            _options = options;
        }

        private static bool ShouldSkipFrame(string? frameInfo) =>
            frameInfo?.StartsWith("Sentry.Ben") == true
            // Skip frames relating to the TaskBlockingListener
            || frameInfo?.StartsWith("System.Diagnostics") == true
            // Skip frames relating to the async state machine
            || frameInfo?.StartsWith("System.Threading") == true;

        public void BlockingStart(DetectionSource detectionSource)
        {
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                return;
            }

            RecursionCount++;

            try
            {
                if (RecursionCount != 1)
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

            RecursionCount--;
        }
    }

    internal enum DetectionSource
    {
        SynchronizationContext,
        EventListener
    }
}
