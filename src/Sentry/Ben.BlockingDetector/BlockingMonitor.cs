using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    internal class BlockingMonitor
    {
        [ThreadStatic]
        internal static int t_recursionCount;

        private readonly Func<IHub> _getHub;
        private readonly SentryOptions _options;
        private readonly TimeSpan _cooldown;

        private static Lazy<Dictionary<string, DateTimeOffset>> _lazyLastReported = new();
        private static Dictionary<string, DateTimeOffset> LastReported => _lazyLastReported.Value;
        private static Lazy<ConcurrentDictionary<string, ReaderWriterLockSlim>> _lazyLastReportedLocks => new();
        private static ConcurrentDictionary<string, ReaderWriterLockSlim> LastReportedLocks => _lazyLastReportedLocks.Value;

        public BlockingMonitor(Func<IHub> getHub, SentryOptions options, TimeSpan? cooldown = null)
        {
            _getHub = getHub;
            _options = options;
            _cooldown = cooldown ?? TimeSpan.FromDays(1);
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

            t_recursionCount++;

            try
            {
                if (t_recursionCount != 1)
                {
                    return;
                }

                var stackTrace = DebugStackTrace.Create(
                    _options,
                    new StackTrace(true),
                    true,
                    ShouldSkipFrame
                );

                // Check if we've seen this code location in the cooldown period
                var lastFrame = stackTrace.Frames.Last();
                var key = $"{lastFrame.Module}::{lastFrame.Function}::{lastFrame.LineNumber}";

                var readWriteLock = LastReportedLocks.GetOrAdd(key, _ => new ReaderWriterLockSlim());
                readWriteLock.EnterUpgradeableReadLock();
                try
                {
                    if (LastReported.TryGetValue(key, out var lastReported) && DateTimeOffset.UtcNow - lastReported < _cooldown)
                    {
                        return;
                    }
                    readWriteLock.EnterWriteLock();
                    try
                    {
                        LastReported[key] = DateTimeOffset.UtcNow;
                    }
                    finally
                    {
                        readWriteLock.ExitWriteLock();
                    }
                }
                finally
                {
                    readWriteLock.ExitUpgradeableReadLock();
                }

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

                _getHub().CaptureEvent(evt);
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
