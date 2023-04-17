using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Extension methods for <see cref="ISentryScopeManager"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryScopeManagerExtensions
{
    /// <summary>
    /// Runs the callback within a new scope.
    /// </summary>
    /// <remarks>
    /// Pushes a new scope, runs the callback, then pops the scope. Use this when you have significant work to
    /// perform within an isolated scope.  If you just need to configure scope for a single event, use the overloads
    /// of CaptureEvent, CaptureMessage and CaptureException that provide a callback to a configurable scope.
    /// </remarks>
    /// <see href="https://docs.sentry.io/platforms/dotnet/enriching-events/scopes/#local-scopes"/>
    /// <param name="scopeManager">The scope manager (usually the hub).</param>
    /// <param name="scopeCallback">The callback to run with the one time scope.</param>
    /// <returns>The result from the callback.</returns>
    [DebuggerStepThrough]
    public static T? WithScope<T>(this ISentryScopeManager scopeManager, Func<Scope, T?> scopeCallback) =>
        scopeManager switch
        {
            Hub hub => hub.ScopeManager.WithScope(scopeCallback),
            IInternalScopeManager manager => manager.WithScope(scopeCallback),
            _ => default
        };

    /// <summary>
    /// Runs the asynchronous callback within a new scope.
    /// </summary>
    /// <remarks>
    /// Asynchronous version of <see cref="ISentryScopeManager.WithScope"/>.
    /// Pushes a new scope, runs the callback, then pops the scope. Use this when you have significant work to
    /// perform within an isolated scope.  If you just need to configure scope for a single event, use the overloads
    /// of CaptureEvent, CaptureMessage and CaptureException that provide a callback to a configurable scope.
    /// </remarks>
    /// <see href="https://docs.sentry.io/platforms/dotnet/enriching-events/scopes/#local-scopes"/>
    /// <param name="scopeManager">The scope manager (usually the hub).</param>
    /// <param name="scopeCallback">The callback to run with the one time scope.</param>
    /// <returns>An async task to await the callback.</returns>
    [DebuggerStepThrough]
    public static Task WithScopeAsync(this ISentryScopeManager scopeManager, Func<Scope, Task> scopeCallback) =>
        scopeManager switch
        {
            Hub hub => hub.ScopeManager.WithScopeAsync(scopeCallback),
            IInternalScopeManager manager => manager.WithScopeAsync(scopeCallback),
            _ => Task.CompletedTask
        };

    /// <summary>
    /// Runs the asynchronous callback within a new scope.
    /// </summary>
    /// <remarks>
    /// Asynchronous version of <see cref="ISentryScopeManager.WithScope"/>.
    /// Pushes a new scope, runs the callback, then pops the scope. Use this when you have significant work to
    /// perform within an isolated scope.  If you just need to configure scope for a single event, use the overloads
    /// of CaptureEvent, CaptureMessage and CaptureException that provide a callback to a configurable scope.
    /// </remarks>
    /// <see href="https://docs.sentry.io/platforms/dotnet/enriching-events/scopes/#local-scopes"/>
    /// <param name="scopeManager">The scope manager (usually the hub).</param>
    /// <param name="scopeCallback">The callback to run with the one time scope.</param>
    /// <returns>An async task to await the result of the callback.</returns>
    [DebuggerStepThrough]
    public static Task<T?> WithScopeAsync<T>(this ISentryScopeManager scopeManager, Func<Scope, Task<T?>> scopeCallback) =>
        scopeManager switch
        {
            Hub hub => hub.ScopeManager.WithScopeAsync(scopeCallback),
            IInternalScopeManager manager => manager.WithScopeAsync(scopeCallback),
            _ => Task.FromResult(default(T))
        };
}
