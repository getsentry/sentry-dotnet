using Sentry.Extensibility;
using Sentry.Internal.ScopeStack;

namespace Sentry.Internal;

internal sealed class SentryScopeManager : IInternalScopeManager
{
    public IScopeStackContainer ScopeStackContainer { get; }

    private readonly SentryOptions _options;

    private KeyValuePair<Scope, ISentryClient>[] DefaultScopeAndClientStack
    {
        get => ScopeStackContainer.Stack ??= NewStack();
        set => ScopeStackContainer.Stack = value;
    }

    private readonly ConcurrentDictionary<object, KeyValuePair<Scope, ISentryClient>[]> _keyedScopeAndClientStack = new();

    // TODO: Need to work out a mechanism to clean these up when the request finishes...
    private KeyValuePair<Scope, ISentryClient>[] GetKeyedScopeStack(object key) => _keyedScopeAndClientStack.GetOrAdd(key, _ => NewStack())!;

    private void SetKeyedScopeStack(object key, KeyValuePair<Scope, ISentryClient>[] value)
    {
        _keyedScopeAndClientStack[key] = value;
    }

    private Func<KeyValuePair<Scope, ISentryClient>[]> NewStack { get; }

    private bool IsGlobalMode => ScopeStackContainer is GlobalScopeStackContainer;

    public SentryScopeManager(SentryOptions options, ISentryClient rootClient)
    {
        ScopeStackContainer = options.ScopeStackContainer ?? (
            options.IsGlobalModeEnabled
                ? new GlobalScopeStackContainer()
                : new AsyncLocalScopeStackContainer());

        _options = options;
        NewStack = () => new[] { new KeyValuePair<Scope, ISentryClient>(new Scope(options), rootClient) };
    }

    public KeyValuePair<Scope, ISentryClient> GetCurrentKeyed(object key)
    {
        return GetKeyedScopeStack(key)[^1];
    }

    private KeyValuePair<Scope, ISentryClient>[] ScopeAndClientStack
    {
        get
        {
            if (_options.ScopeKeyResolver?.ScopeKey is { } key)
            {
                return GetKeyedScopeStack(key);
            }
            return DefaultScopeAndClientStack;
        }
        set
        {
            if (_options.ScopeKeyResolver?.ScopeKey is { } key)
            {
                SetKeyedScopeStack(key, value);
            }
            else
            {
                DefaultScopeAndClientStack = value;
            }
        }
    }

    public KeyValuePair<Scope, ISentryClient> GetCurrent() => ScopeAndClientStack[^1];

    public void ConfigureScope(Action<Scope>? configureScope)
    {
        var (scope, _) = GetCurrent();
        configureScope?.Invoke(scope);
    }

    public Task ConfigureScopeAsync(Func<Scope, Task>? configureScope)
    {
        var (scope, _) = GetCurrent();
        return configureScope?.Invoke(scope) ?? Task.CompletedTask;
    }

    public IDisposable PushScope() => PushScope<object>(null);

    public IDisposable PushScope<TState>(TState? state)
    {
        if (IsGlobalMode)
        {
            _options.LogWarning("Push scope called in global mode, returning.");
            return DisabledHub.Instance;
        }

        var currentScopeAndClientStack = ScopeAndClientStack;
        var scope = currentScopeAndClientStack[^1];

        if (scope.Key.Locked)
        {
            _options.LogDebug("Locked scope. No new scope pushed.");

            // Apply to current scope
            if (state != null)
            {
                scope.Key.Apply(state);
            }

            return DisabledHub.Instance;
        }

        var clonedScope = scope.Key.Clone();

        if (state != null)
        {
            clonedScope.Apply(state);
        }

        var scopeSnapshot = new ScopeSnapshot(_options, currentScopeAndClientStack, this);

        _options.LogDebug("New scope pushed.");
        var newScopeAndClientStack = new KeyValuePair<Scope, ISentryClient>[currentScopeAndClientStack.Length + 1];
        Array.Copy(currentScopeAndClientStack, newScopeAndClientStack, currentScopeAndClientStack.Length);
        newScopeAndClientStack[^1] = new KeyValuePair<Scope, ISentryClient>(clonedScope, scope.Value);

        ScopeAndClientStack = newScopeAndClientStack;
        return scopeSnapshot;
    }
    public void RestoreScope(Scope savedScope)
    {
        var currentScopeAndClientStack = ScopeAndClientStack;
        var (previousScope, client) = currentScopeAndClientStack[^1];

        _options.LogDebug("Scope restored");
        var newScopeAndClientStack = new KeyValuePair<Scope, ISentryClient>[currentScopeAndClientStack.Length + 1];
        Array.Copy(currentScopeAndClientStack, newScopeAndClientStack, currentScopeAndClientStack.Length);
        newScopeAndClientStack[^1] = new KeyValuePair<Scope, ISentryClient>(savedScope, client);

        ScopeAndClientStack = newScopeAndClientStack;
    }

    public void WithScope(Action<Scope> scopeCallback)
    {
        using (PushScope())
        {
            var (scope, _) = GetCurrent();
            scopeCallback.Invoke(scope);
        }
    }

    public T? WithScope<T>(Func<Scope, T?> scopeCallback)
    {
        using (PushScope())
        {
            var (scope, _) = GetCurrent();
            return scopeCallback.Invoke(scope);
        }
    }

    public async Task WithScopeAsync(Func<Scope, Task> scopeCallback)
    {
        using (PushScope())
        {
            var (scope, _) = GetCurrent();
            await scopeCallback.Invoke(scope).ConfigureAwait(false);
        }
    }

    public async Task<T?> WithScopeAsync<T>(Func<Scope, Task<T?>> scopeCallback)
    {
        using (PushScope())
        {
            var (scope, _) = GetCurrent();
            return await scopeCallback.Invoke(scope).ConfigureAwait(false);
        }
    }

    public void BindClient(ISentryClient? client)
    {
        _options.LogDebug("Binding a new client to the current scope.");

        var currentScopeAndClientStack = ScopeAndClientStack;
        var top = currentScopeAndClientStack[^1];

        var newScopeAndClientStack = new KeyValuePair<Scope, ISentryClient>[currentScopeAndClientStack.Length];
        Array.Copy(currentScopeAndClientStack, newScopeAndClientStack, currentScopeAndClientStack.Length);
        newScopeAndClientStack[^1] = new KeyValuePair<Scope, ISentryClient>(top.Key, client ?? DisabledHub.Instance);
        ScopeAndClientStack = newScopeAndClientStack;
    }

    private sealed class ScopeSnapshot : IDisposable
    {
        private readonly SentryOptions _options;
        private readonly KeyValuePair<Scope, ISentryClient>[] _snapshot;
        private readonly SentryScopeManager _scopeManager;

        public ScopeSnapshot(
            SentryOptions options,
            KeyValuePair<Scope, ISentryClient>[] snapshot,
            SentryScopeManager scopeManager)
        {
            _options = options;
            _snapshot = snapshot;
            _scopeManager = scopeManager;
        }

        public void Dispose()
        {
            _options.LogDebug("Disposing scope.");

            var previousScopeKey = _snapshot[^1].Key;
            var currentScope = _scopeManager.ScopeAndClientStack;

            // Only reset the parent if this is still the current scope
            for (var i = currentScope.Length - 1; i >= 0; --i)
            {
                if (ReferenceEquals(currentScope[i].Key, previousScopeKey))
                {
                    _scopeManager.ScopeAndClientStack = _snapshot;
                    break;
                }
            }
        }
    }

    public void Dispose()
    {
        _options.LogDebug($"Disposing {nameof(SentryScopeManager)}.");
        ScopeStackContainer.Stack = null;
    }
}
