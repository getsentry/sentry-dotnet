using Sentry;
using Sentry.Internal;
using Sentry.Protocol;

namespace Ben.Diagnostics
{
    internal class BlockingMonitor
    {
        private readonly Func<IHub> _getHub;
        private readonly SentryOptions _options;

        [ThreadStatic]
        private static int t_recursionCount;

        public BlockingMonitor(Func<IHub> getHub, SentryOptions options)
        {
            _getHub = getHub;
            _options = options;
        }


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
                        Message = "Blocking method has been invoked and blocked, this can lead to threadpool starvation.",
                        SentryExceptions = new[]
                        {
                            new SentryException
                            {
                                Type = "Blocking call detected",
                                Stacktrace = DebugStackTrace.Create(
                                    _options,
                                    // TODO: originally it was 3 frames from here to skip with sync matchging:
                                    new StackTrace(detectionSource == DetectionSource.SynchronizationContext ? 5 : 6),
                                    false),
                            }
                        }
                    };

                    // TODO: How to render in the UI a better "suggested fix"?
                    evt.SetExtra(
                        "suggestion",
                        "Analyzer to warn you from blocking calls on async flows; https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers/");

                    _getHub().CaptureEvent(evt);
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
