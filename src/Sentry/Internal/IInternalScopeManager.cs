using System;

namespace Sentry.Internal
{
    internal interface IInternalScopeManager : ISentryScopeManager
    {
        Tuple<Scope, ISentryClient> GetCurrent();
    }
}
