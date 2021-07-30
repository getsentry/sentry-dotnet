using System;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry
{
    // AKA server mode
    // https://develop.sentry.dev/sdk/sessions
    internal class ServerSessionManager : ISessionManager
    {
        private readonly SentryOptions _options;
        private readonly ISentryClient _client;
        private readonly IInternalScopeManager _scopeManager;
        private readonly ISystemClock _clock;

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
    }
}
