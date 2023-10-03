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
    /// Runs the callback within a new scope.
    /// </summary>
    /// <remarks>
    /// Pushes a new scope, runs the callback, then pops the scope. Use this when you have significant work to
    /// perform within an isolated scope.  If you just need to configure scope for a single event, use the overloads
    /// of CaptureEvent, CaptureMessage and CaptureException that provide a callback to a configurable scope.
    /// </remarks>
    /// <see href="https://docs.sentry.io/platforms/dotnet/enriching-events/scopes/#local-scopes"/>
    /// <param name="scopeCallback">The callback to run with the one time scope.</param>
    [Obsolete("This method is deprecated in favor of overloads of CaptureEvent, CaptureMessage and CaptureException " +
              "that provide a callback to a configurable scope.")]
    void WithScope(Action<Scope> scopeCallback);
}
