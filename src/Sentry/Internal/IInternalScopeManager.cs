using System.Collections.Generic;

namespace Sentry.Internal
{
    internal interface IInternalScopeManager : ISentryScopeManager
    {
        KeyValuePair<Scope, ISentryClient> GetCurrent();
    }
}
