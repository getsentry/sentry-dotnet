namespace Sentry;

internal interface ISessionManager
{
    public bool IsSessionActive { get; }

    public SessionUpdate? TryRecoverPersistedSession();

    public SessionUpdate? StartSession();

    public SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status);

    public SessionUpdate? EndSession(SessionEndStatus status);

    public void PauseSession();

    public IReadOnlyList<SessionUpdate> ResumeSession();

    public SessionUpdate? ReportError();
}
