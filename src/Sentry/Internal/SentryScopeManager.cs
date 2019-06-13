using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class SentryScopeManager : IInternalScopeManager, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly AsyncLocal<ConcurrentStack<(Scope, ISentryClient)>> _asyncLocalScope = new AsyncLocal<ConcurrentStack<(Scope, ISentryClient)>>();

        internal ConcurrentStack<(Scope scope, ISentryClient client)> ScopeAndClientStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = NewStack());
            set => _asyncLocalScope.Value = value;
        }

        private Func<ConcurrentStack<(Scope, ISentryClient)>> NewStack { get; }

        public SentryScopeManager(
            SentryOptions options,
            ISentryClient rootClient)
        {
            Debug.Assert(rootClient != null);
            _options = options;
            NewStack = () => new ConcurrentStack<(Scope, ISentryClient)>(new[] { (new Scope(options), rootClient) });
        }

        public (Scope Scope, ISentryClient Client) GetCurrent()
        {
            ScopeAndClientStack.TryPeek(out var tuple);
            return tuple;
        } 

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
            currentScopeAndClientStack.TryPeek(out var tuple);
            var (scope, client) = tuple;

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
            currentScopeAndClientStack.Push((clonedScope, client));

            return scopeSnapshot;
        }

        public void WithScope(Action<Scope> scopeCallback)
        {
            using (PushScope())
            {
                var scope = GetCurrent();
                scopeCallback?.Invoke(scope.Scope);
            }
        }

        public void BindClient(ISentryClient client)
        {
            _options?.DiagnosticLogger?.LogDebug("Binding a new client to the current scope.");

            var currentScopeAndClientStack = ScopeAndClientStack;
            currentScopeAndClientStack.TryPop(out var top);
            currentScopeAndClientStack.Push((top.scope, client ?? DisabledHub.Instance));
        }

        private class ScopeSnapshot : IDisposable
        {
            private readonly SentryOptions _options;
            private readonly ConcurrentStack<(Scope scope, ISentryClient client)> _snapshot;
            private readonly SentryScopeManager _scopeManager;

            public ScopeSnapshot(
                SentryOptions options,
                ConcurrentStack<(Scope, ISentryClient)> snapshot,
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
                    _snapshot.TryPeek(out var tuple);
                    if (ReferenceEquals(scope, tuple.scope))
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
