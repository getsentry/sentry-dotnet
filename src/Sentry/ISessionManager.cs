using System;

namespace Sentry
{
    internal interface ISessionManager
    {
        bool IsSessionActive { get; }

        SessionUpdate? StartSession();

        SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status);

        SessionUpdate? EndSession(SessionEndStatus status);

        SessionUpdate? ReportError();
    }
}
