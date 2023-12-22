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
                        Message = "Blocking method has been invoked and blocked, this can lead to ThreadPool starvation.",
                        SentryExceptions = new[]
                        {
                            new SentryException
                            {
                                Type = "Blocking call detected",
                                Stacktrace = DebugStackTrace.Create(
                                    options,
                                    new StackTrace(),
                                    true,
                                    // Skip frames once the Sentry frames are already removed
                                    detectionSource == DetectionSource.SynchronizationContext ? 0 : 3),
                                // detectionSource == DetectionSource.SynchronizationContext ? 3 : 6),
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
