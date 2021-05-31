namespace Sentry
{
    internal interface ISessionManager
    {
        Session? CurrentSession { get; }

        Session? StartSession();

        Session? EndSession(SessionEndState state);
    }
}
