using System;

namespace Sentry
{
    internal interface ISessionManager
    {
        SessionUpdate? StartSession();

        SessionUpdate? EndSession(SessionEndStatus status, DateTimeOffset timestamp);

        SessionUpdate? ReportError();
    }
}
