using Sentry.Internal.ScopeStack;

namespace Sentry.Internal;

internal interface IInternalScopeManager : ISentryScopeManager, IDisposable
{
    KeyValuePair<Scope, ISentryClient> GetCurrent();
    void RestoreScope(Scope savedScope);

    IScopeStackContainer ScopeStackContainer { get; }
}
