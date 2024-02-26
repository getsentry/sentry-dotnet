#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
#endif
using Sentry.Internal;
using Sentry.Protocol;

// Namespace starting with Sentry makes sure the SDK cuts frames off before reporting
namespace Sentry.Ben.BlockingDetector
{
    internal class BlockingMonitor
    {
#if NET8_0_OR_GREATER
        private static readonly Lazy<Counter<int>> LazyBlockingCallsDetected = new(() =>
            SentryMeters.BlockingDetectorMeter.CreateCounter<int>("sentry.blocking_calls_detected"));
        private static Counter<int> BlockingCallsDetected => LazyBlockingCallsDetected.Value;
#endif

        [ThreadStatic]
        internal static int RecursionCount;

        private readonly Func<IHub> _getHub;
        private readonly SentryOptions _options;
        private readonly TimeSpan _cooldown;

        private static readonly Lazy<Dictionary<string, DateTimeOffset>> LazyLastReported = new();
        private static Dictionary<string, DateTimeOffset> LastReported => LazyLastReported.Value;
        private static Lazy<ConcurrentDictionary<string, ReaderWriterLockSlim>> LazyLastReportedLocks => new();
        private static ConcurrentDictionary<string, ReaderWriterLockSlim> LastReportedLocks => LazyLastReportedLocks.Value;

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

                var lastFrame = stackTrace.Frames.Last();
                var locationId = $"{lastFrame.Module}::{lastFrame.Function}::{lastFrame.LineNumber}::{lastFrame.ColumnNumber}";

#if NET8_0_OR_GREATER
                BlockingCallsDetected.Add(1, new KeyValuePair<string, object?>("location", locationId));
#endif

                // Check if we've seen this code location in the cooldown period
                var readWriteLock = LastReportedLocks.GetOrAdd(locationId, _ => new ReaderWriterLockSlim());
                readWriteLock.EnterUpgradeableReadLock();
                try
                {
                    if (LastReported.TryGetValue(locationId, out var lastReported) && DateTimeOffset.UtcNow - lastReported < _cooldown)
                    {
                        return;
                    }
                    readWriteLock.EnterWriteLock();
                    try
                    {
                        LastReported[locationId] = DateTimeOffset.UtcNow;
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
