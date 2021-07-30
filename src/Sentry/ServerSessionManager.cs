using System;
using System.Timers;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry
{
    // AKA server mode
    // https://develop.sentry.dev/sdk/sessions
    internal class ServerSessionManager : ISessionManager, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly ISentryClient _client;
        private readonly IInternalScopeManager _scopeManager;
        private readonly ISystemClock _clock;
        private readonly Timer _timer;

        public ServerSessionManager(
            SentryOptions options,
            ISentryClient client,
            IInternalScopeManager scopeManager,
            ISystemClock clock)
        {
            _options = options;
            _client = client;
            _scopeManager = scopeManager;
            _clock = clock;

            _timer = new Timer {Interval = 60 * 1000, Enabled = true, AutoReset = true};
            _timer.Elapsed += (_, _) => Flush();
        }

        private void Flush()
        {
            try
            {
                // TODO: aggregate and flush active sessions
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
            throw new NotImplementedException();
        }

        public void EndSession(SessionEndStatus status)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Dispose() => _timer.Dispose();
    }
}
