using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal interface IInternalScopeManagement : ISentryScopeManagement
    {
        (Scope Scope, ISentryClient Client) GetCurrent();
    }
}
