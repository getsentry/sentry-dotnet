using Sentry.Internal.ScopeStack;

namespace Sentry.Internal;

internal interface IInternalScopeManager : ISentryScopeManager, IDisposable
{
    public KeyValuePair<Scope, ISentryClient> GetCurrent();
    public void RestoreScope(Scope savedScope);

    public IScopeStackContainer ScopeStackContainer { get; }
}
