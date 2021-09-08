using System;
using System.Timers;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry
{
    // Strategy for future reference:
    // - Keep only one set of sessions in memory
    // - Make the timer run on-the-minute
    // - Just track the exited/error count
    // - When the timer hits - flush

    // AKA server mode
    // https://develop.sentry.dev/sdk/sessions
    internal class ServerSessionManager : ISessionManager
    {
        private readonly object _lock = new();

        private readonly SentryOptions _options;
        private readonly ISentryClient _client;
        private readonly ISystemClock _clock;
        private readonly Timer _timer;

        // Internal for testing.
        internal int ExitedCount { get; private set; }
        // Internal for testing.
        internal int ErroredCount { get; private set; }

        public ServerSessionManager(
            SentryOptions options,
            ISentryClient client,
            ISystemClock clock)
        {
            _options = options;
            _client = client;
            _clock = clock;
            _timer = new Timer { Interval = TimeSpan.FromMinutes(1).TotalMilliseconds, Enabled = true, AutoReset = true };
            _timer.Elapsed += (_, _) => Flush();
        }

        private SessionAggregate? TryAggregate()
        {
            lock (_lock)
            {
                // If no sessions exited or errored, that means none were started during this period
                if (ExitedCount == 0 && ErroredCount == 0)
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "No sessions to aggregate."
                    );

                    return null;
                }

                // Extract release
                var release = ReleaseLocator.Resolve(_options);
                if (string.IsNullOrWhiteSpace(release))
                {
                    // Release health without release is just health (useless)
                    _options.DiagnosticLogger?.LogError(
                        "Failed to aggregate sessions because there is no release information."
                    );

                    return null;
                }

                // Extract environment
                var environment = EnvironmentLocator.Resolve(_options);

                // Get now rounded down to the current minute
                var now = _clock.GetUtcNow();
                var startTimestamp = new DateTimeOffset(
                    now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset
                );

                var aggregate = new SessionAggregate(
                    startTimestamp, ExitedCount, ErroredCount, release, environment
                );

                // Reset values
                ExitedCount = 0;
                ErroredCount = 0;

                return aggregate;
            }
        }

        // Internal for testing.
        internal void Flush()
        {
            try
            {
                if (TryAggregate() is { } aggregate)
                {
                    _client.CaptureSessionAggregate(aggregate);
                    _options.DiagnosticLogger?.LogInfo("Flushed a session aggregate.");
                }
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to flush sessions in server session manager.",
                    ex
                );
            }
        }

        public void StartSession()
        {
            // No-op
        }

        public void EndSession(SessionEndStatus status)
        {
            lock (_lock)
            {
                // Note:
                // Need to make sure status is correctly set.
                // Session aggregates don't discern between Crashed and Abnormal.
                // So we just count them both as errored.
                if (status == SessionEndStatus.Exited)
                {
                    ExitedCount++;
                }
                else
                {
                    ErroredCount++;
                }
            }
        }

        public void PauseSession()
        {
            // No-op
        }

        public void ResumeSession()
        {
            // No-op
        }

        public void ReportError()
        {
            // No-op
        }

        public void Dispose()
        {
            _timer.Elapsed -= (_, _) => Flush();
            _timer.Dispose();
        }
    }
}
