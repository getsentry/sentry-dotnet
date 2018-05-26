using Sentry.Protocol;

namespace Sentry
{
    internal interface IInternalScopeManagement : ISentryScopeManagement
    {
        Scope GetCurrent();
    }
}