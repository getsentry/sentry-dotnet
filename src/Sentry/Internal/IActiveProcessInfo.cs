namespace Sentry.Internal
{
    internal interface IActiveProcessInfo
    {
        int GetCurrentProcessId();
        bool IsProcessActive(int processId);
    }
}
