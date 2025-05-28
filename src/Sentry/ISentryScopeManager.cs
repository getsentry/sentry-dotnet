namespace Sentry;

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
    public void ConfigureScope(Action<Scope> configureScope);

    /// <summary>
    /// Asynchronously configure the current scope.
    /// </summary>
    /// <param name="configureScope">The configure scope.</param>
    /// <returns>A task that completes when the callback is done or a completed task if the SDK is disabled.</returns>
    public Task ConfigureScopeAsync(Func<Scope, Task> configureScope);

    /// <summary>
    /// Binds the client to the current scope.
    /// </summary>
    /// <param name="client">The client.</param>
    public void BindClient(ISentryClient client);

    /// <summary>
    /// Pushes a new scope into the stack which is removed upon Dispose.
    /// </summary>
    /// <returns>A disposable which removes the scope
    /// from the environment when invoked.</returns>
    public IDisposable PushScope();

    /// <summary>
    /// Pushes a new scope into the stack which is removed upon Dispose.
    /// </summary>
    /// <param name="state">A state to associate with the scope.</param>
    /// <returns>A disposable which removes the scope
    /// from the environment when invoked.</returns>
    public IDisposable PushScope<TState>(TState state);
}
