using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class SentryScopeManager : IInternalScopeManager, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly AsyncLocal<ImmutableStack<(Scope, ISentryClient)>> _asyncLocalScope = new AsyncLocal<ImmutableStack<(Scope, ISentryClient)>>();

        internal ImmutableStack<(Scope scope, ISentryClient client)> ScopeAndClientStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = NewStack());
            set => _asyncLocalScope.Value = value;
        }

        private Func<ImmutableStack<(Scope, ISentryClient)>> NewStack { get; }

        public SentryScopeManager(
            SentryOptions options,
            ISentryClient rootClient)
        {
            Debug.Assert(rootClient != null);
            _options = options;
            NewStack = () => ImmutableStack.Create((new Scope(options), rootClient));
        }

        public (Scope Scope, ISentryClient Client) GetCurrent() => ScopeAndClientStack.Peek();

        public void ConfigureScope(Action<Scope> configureScope)
        {
            _options?.DiagnosticLogger?.LogDebug("Configuring the scope.");
            var scope = GetCurrent();
            configureScope?.Invoke(scope.Scope);
        }

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        {
            _options?.DiagnosticLogger?.LogDebug("Configuring the scope asynchronously.");
            var scope = GetCurrent();
            return configureScope?.Invoke(scope.Scope) ?? Task.CompletedTask;
        }

        public IDisposable PushScope() => PushScope<object>(null);

        public IDisposable PushScope<TState>(TState state)
        {
            var currentScopeAndClientStack = ScopeAndClientStack;
            var (scope, client) = currentScopeAndClientStack.Peek();

            if (scope.Locked)
            {
                // TODO: keep state on current scope?
                _options?.DiagnosticLogger?.LogDebug("Locked scope. No new scope pushed.");
                return DisabledHub.Instance;
            }

            var clonedScope = scope.Clone();
            if (state != null)
            {
                clonedScope.Apply(state);
            }
            var scopeSnapshot = new ScopeSnapshot(_options, currentScopeAndClientStack, this);
            _options?.DiagnosticLogger?.LogDebug("New scope pushed.");
            ScopeAndClientStack = currentScopeAndClientStack.Push((clonedScope, client));

            return scopeSnapshot;
        }

        public void BindClient(ISentryClient client)
        {
            _options?.DiagnosticLogger?.LogDebug("Binding a new client to the current scope.");

            var currentScopeAndClientStack = ScopeAndClientStack;
            currentScopeAndClientStack = currentScopeAndClientStack.Pop(out var top);
            currentScopeAndClientStack = currentScopeAndClientStack.Push((top.scope, client ?? DisabledHub.Instance));
            ScopeAndClientStack = currentScopeAndClientStack;
        }

        private class ScopeSnapshot : IDisposable
        {
            private readonly SentryOptions _options;
            private readonly ImmutableStack<(Scope scope, ISentryClient client)> _snapshot;
            private readonly SentryScopeManager _scopeManager;

            public ScopeSnapshot(
                SentryOptions options,
                ImmutableStack<(Scope, ISentryClient)> snapshot,
                SentryScopeManager scopeManager)
            {
                Debug.Assert(snapshot != null);
                Debug.Assert(scopeManager != null);
                _options = options;
                _snapshot = snapshot;
                _scopeManager = scopeManager;
            }

            public void Dispose()
            {
                _options?.DiagnosticLogger?.LogDebug("Disposing scope.");

                // Only reset the parent if this is still the current scope
                foreach (var (scope, _) in _scopeManager.ScopeAndClientStack)
                {
                    if (ReferenceEquals(scope, _snapshot.Peek().scope))
                    {
                        _scopeManager.ScopeAndClientStack = _snapshot;
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            _options?.DiagnosticLogger?.LogDebug("Disposing SentryClient.");
            _asyncLocalScope.Value = null;
        }
    }
}
