using Sentry.Internal.ScopeStack;

namespace Sentry.Internal;

internal interface IInternalScopeManager : ISentryScopeManager, IDisposable
{
    KeyValuePair<Scope, ISentryClient> GetCurrent();
    void RestoreScope(Scope savedScope);

    IScopeStackContainer ScopeStackContainer { get; }

    // TODO: Move The following to ISentryScopeManager in a future major version.
    T? WithScope<T>(Func<Scope, T?> scopeCallback);
    Task WithScopeAsync(Func<Scope, Task> scopeCallback);
    Task<T?> WithScopeAsync<T>(Func<Scope, Task<T?>> scopeCallback);
}
