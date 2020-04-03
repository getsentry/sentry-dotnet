using System;

namespace Sentry.Internal
{
    internal interface IInternalScopeManager : ISentryScopeManager
    {
        ValueTuple<Scope, ISentryClient> GetCurrent();
    }
}
