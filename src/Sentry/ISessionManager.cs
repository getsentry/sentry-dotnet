using System;

namespace Sentry
{
    internal interface ISessionManager
    {
        SessionUpdate? StartSession();

        SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status);

        SessionUpdate? EndSession(SessionEndStatus status);

        SessionUpdate? ReportError();
    }
}
