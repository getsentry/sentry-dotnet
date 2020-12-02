using System;
using System.Threading.Tasks;

namespace Sentry
{
    /// <summary>
    /// Scope management.
    /// </summary>
    /// <remarks>
    /// An implementation shall create new scopes and allow consumers
    /// modify the current scope.
    /// </remarks>
    public interface ISentryScopeManager
    {
        /// <summary>
        /// Configures the current scope.
        /// </summary>
        /// <param name="configureScope">The configure scope.</param>
        void ConfigureScope(Action<Scope> configureScope);

        /// <summary>
        /// Asynchronously configure the current scope.
        /// </summary>
        /// <param name="configureScope">The configure scope.</param>
        /// <returns>A task that completes when the callback is done or a completed task if the SDK is disabled.</returns>
        Task ConfigureScopeAsync(Func<Scope, Task> configureScope);

        /// <summary>
        /// Binds the client to the current scope.
        /// </summary>
        /// <param name="client">The client.</param>
        void BindClient(ISentryClient client);

        /// <summary>
        /// Pushes a new scope into the stack which is removed upon Dispose.
        /// </summary>
        /// <returns>A disposable which removes the scope
        /// from the environment when invoked.</returns>
        IDisposable PushScope();

        /// <summary>
        /// Pushes a new scope into the stack which is removed upon Dispose.
        /// </summary>
        /// <param name="state">A state to associate with the scope.</param>
        /// <returns>A disposable which removes the scope
        /// from the environment when invoked.</returns>
        IDisposable PushScope<TState>(TState state);

        /// <summary>
        /// Runs the callback with a new scope which gets dropped at the end.
        /// </summary>
        /// <remarks>
        /// Pushes a new scope, runs the callback, pops the scope.
        /// </remarks>
        /// <see href="https://docs.sentry.io/platforms/dotnet/enriching-events/scopes/#local-scopes"/>
        /// <param name="scopeCallback">The callback to run with the one time scope.</param>
        void WithScope(Action<Scope> scopeCallback);
    }
}
