namespace Sentry
{
    internal interface ISessionManager
    {
        SessionUpdate? StartSession();

        SessionUpdate? ReportError();

        SessionUpdate? EndSession(SessionEndStatus status);
    }
}
