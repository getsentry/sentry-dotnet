namespace Sentry;

internal interface ISessionManager
{
    bool IsSessionActive { get; }

    SessionUpdate? TryRecoverPersistedSession();

    SessionUpdate? StartSession();

    SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status);

    SessionUpdate? EndSession(SessionEndStatus status);

    void PauseSession();

    IReadOnlyList<SessionUpdate> ResumeSession();

    SessionUpdate? ReportError();
}
