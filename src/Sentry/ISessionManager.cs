using System;

namespace Sentry
{
    internal interface ISessionManager : IDisposable
    {
        void StartSession();

        void EndSession(SessionEndStatus status);

        void PauseSession();

        void ResumeSession();

        void ReportError();
    }
}
