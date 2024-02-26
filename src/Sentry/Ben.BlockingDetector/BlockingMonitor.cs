using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    internal class BlockingMonitor(Func<IHub> getHub, SentryOptions options)
    {
        [ThreadStatic]
        internal static int t_recursionCount;

        private static bool ShouldSkipFrame(string? frameInfo) =>
            frameInfo?.StartsWith("Sentry.Ben") == true
            // Skip frames relating to the TaskBlockingListener
            || frameInfo?.StartsWith("System.Diagnostics") == true
            // Skip frames relating to the async state machine
            || frameInfo?.StartsWith("System.Threading") == true;

        public void BlockingStart(DetectionSource detectionSource)
        {
            options.LogDebug("BlockingStart: Recursion count: {0}", t_recursionCount);
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                options.LogDebug("BlockingStart: !IsThreadPoolThread");
                return;
            }

            t_recursionCount++;

            try
            {
                if (t_recursionCount != 1)
                {
                    return;
                }

                var stackTrace = DebugStackTrace.Create(
                    options,
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
                    // SentryThreads = new []{new SentryThread
                    //     {
                    //         Id = Environment.CurrentManagedThreadId,
                    //     }},
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

                getHub().CaptureEvent(evt);
            }
            catch
            {
            }
        }

        public void BlockingEnd()
        {
            options.LogDebug("BlockingStart: Recursion count: {0}", t_recursionCount);
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                options.LogDebug("BlockingStart: !IsThreadPoolThread");
                return;
            }

            t_recursionCount--;
        }
    }

    internal enum DetectionSource
    {
        SynchronizationContext,
        EventListener
    }
}
