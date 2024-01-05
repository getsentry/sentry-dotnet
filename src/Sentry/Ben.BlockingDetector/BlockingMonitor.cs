using Sentry.Internal;
using Sentry.Protocol;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.Diagnostics
{
    internal class BlockingMonitor(Func<IHub> getHub, SentryOptions options)
    {
        [ThreadStatic]
        private static int t_recursionCount;

        public void BlockingStart(DetectionSource detectionSource)
        {
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
                return;
            }

            t_recursionCount++;

            try
            {
                if (t_recursionCount == 1)
                {
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
                                Stacktrace = DebugStackTrace.Create(
                                    options,
                                    new StackTrace(true),
                                    true,
                                    // Skip frames once the Sentry frames are already removed
                                    // Originally this skipped 3 more frames (context ? 3 : 6) but since Sentry hides the System frames
                                    // by default, this will focus on the App frame but allow the user to see a bit deeper
                                    // into what .NET does for blocking.
                                    detectionSource == DetectionSource.SynchronizationContext ? 0 : 3),
                            }
                        },
                    };

                    // TODO: How to render in the UI a better "suggested fix"?
                    evt.SetExtra(
                        "suggestion",
                        "Analyzer to warn you from blocking calls on async flows; https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers/");

                    getHub().CaptureEvent(evt);
                }
            }
            catch
            {
            }
        }

        public void BlockingEnd()
        {
            if (!Thread.CurrentThread.IsThreadPoolThread)
            {
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
