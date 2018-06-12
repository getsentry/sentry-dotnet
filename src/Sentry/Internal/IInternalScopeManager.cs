using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal interface IInternalScopeManager : ISentryScopeManager
    {
        (Scope Scope, ISentryClient Client) GetCurrent();
    }
}
