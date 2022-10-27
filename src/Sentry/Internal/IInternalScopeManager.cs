using Sentry.Internal.ScopeStack;

namespace Sentry.Internal;

internal interface IInternalScopeManager : ISentryScopeManager, IDisposable
{
    KeyValuePair<Scope, ISentryClient> GetCurrent();
    IScopeStackContainer ScopeStackContainer { get; }
}
