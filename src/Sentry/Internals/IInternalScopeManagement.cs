using Sentry.Protocol;

namespace Sentry.Internals
{
    internal interface IInternalScopeManagement : ISentryScopeManagement
    {
        Scope GetCurrent();
    }
}
