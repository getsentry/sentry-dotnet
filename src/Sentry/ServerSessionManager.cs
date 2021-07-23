using System;
using System.Collections.Generic;
using System.Threading;

namespace Sentry
{
    // AKA server mode
    // https://develop.sentry.dev/sdk/sessions
    internal class ServerSessionManager : ISessionManager
    {
        private readonly AsyncLocal<Session?> _sessionContainer = new();

        private Session? Session
        {
            get => _sessionContainer.Value;
            set => _sessionContainer.Value = value;
        }

        public bool IsSessionActive => Session is not null;

        public SessionUpdate? TryRecoverPersistedSession() => null;

        public SessionUpdate? StartSession()
        {
            throw new NotImplementedException();
        }

        public SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status)
        {
            throw new NotImplementedException();
        }

        public SessionUpdate? EndSession(SessionEndStatus status)
        {
            throw new NotImplementedException();
        }

        public void PauseSession()
        {
        }

        public IReadOnlyList<SessionUpdate> ResumeSession() => Array.Empty<SessionUpdate>();

        public SessionUpdate? ReportError()
        {
            throw new NotImplementedException();
        }
    }
}
