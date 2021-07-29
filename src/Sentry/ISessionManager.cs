namespace Sentry
{
    internal interface ISessionManager
    {
        void StartSession();

        void EndSession(SessionEndStatus status);

        void PauseSession();

        void ResumeSession();

        void ReportError();
    }
}
