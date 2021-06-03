namespace Sentry
{
    internal interface ISessionManager
    {
        Session? StartSession();

        void ReportError();

        Session? EndSession(SessionEndStatus status);
    }
}
