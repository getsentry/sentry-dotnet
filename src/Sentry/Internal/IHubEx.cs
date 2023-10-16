namespace Sentry.Internal;

internal interface IHubEx : IHub
{
    SentryId CaptureEventInternal(SentryEvent evt, Hint? hint, Scope? scope = null);

    T? WithScope<T>(Func<Scope, T?> scopeCallback);
    Task WithScopeAsync(Func<Scope, Task> scopeCallback);
    Task<T?> WithScopeAsync<T>(Func<Scope, Task<T?>> scopeCallback);
}
