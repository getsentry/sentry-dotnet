using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal sealed class SentryScopeManager : IInternalScopeManager, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly AsyncLocal<KeyValuePair<Scope, ISentryClient>[]?> _asyncLocalScope = new();

        internal KeyValuePair<Scope, ISentryClient>[] ScopeAndClientStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = NewStack());
            set => _asyncLocalScope.Value = value;
        }

        private Func<KeyValuePair<Scope, ISentryClient>[]> NewStack { get; }

        public SentryScopeManager(
            SentryOptions options,
            ISentryClient rootClient)
        {
            _options = options;
            NewStack = () => new [] { new KeyValuePair<Scope, ISentryClient>(new Scope(options), rootClient) };
        }

        public KeyValuePair<Scope, ISentryClient> GetCurrent()
        {
            var current = ScopeAndClientStack;
            return current[current.Length - 1];
        }

        public void ConfigureScope(Action<Scope>? configureScope)
        {
            _options.DiagnosticLogger?.LogDebug("Configuring the scope.");
            var scope = GetCurrent();
            configureScope?.Invoke(scope.Key);
        }

        public Task ConfigureScopeAsync(Func<Scope, Task>? configureScope)
        {
            _options.DiagnosticLogger?.LogDebug("Configuring the scope asynchronously.");
            var scope = GetCurrent();
            return configureScope?.Invoke(scope.Key) ?? Task.CompletedTask;
        }

        public IDisposable PushScope() => PushScope<object>(null!); // NRTs don't work well with generics

        public IDisposable PushScope<TState>(TState state)
        {
            var currentScopeAndClientStack = ScopeAndClientStack;
            var scope = currentScopeAndClientStack[currentScopeAndClientStack.Length - 1];

            if (scope.Key.Locked)
            {
                _options.DiagnosticLogger?.LogDebug("Locked scope. No new scope pushed.");

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

            _options.DiagnosticLogger?.LogDebug("New scope pushed.");
            var newScopeAndClientStack = new KeyValuePair<Scope, ISentryClient>[currentScopeAndClientStack.Length + 1];
            Array.Copy(currentScopeAndClientStack, newScopeAndClientStack, currentScopeAndClientStack.Length);
            newScopeAndClientStack[newScopeAndClientStack.Length - 1] = new KeyValuePair<Scope, ISentryClient>(clonedScope, scope.Value);

            ScopeAndClientStack = newScopeAndClientStack;
            return scopeSnapshot;
        }

        public void WithScope(Action<Scope> scopeCallback)
        {
            using (PushScope())
            {
                var scope = GetCurrent();
                scopeCallback.Invoke(scope.Key);
            }
        }

        public void BindClient(ISentryClient? client)
        {
            _options.DiagnosticLogger?.LogDebug("Binding a new client to the current scope.");

            var currentScopeAndClientStack = ScopeAndClientStack;
            var top = currentScopeAndClientStack[currentScopeAndClientStack.Length - 1];

            var newScopeAndClientStack = new KeyValuePair<Scope, ISentryClient>[currentScopeAndClientStack.Length];
            Array.Copy(currentScopeAndClientStack, newScopeAndClientStack, currentScopeAndClientStack.Length);
            newScopeAndClientStack[newScopeAndClientStack.Length - 1] = new KeyValuePair<Scope, ISentryClient>(top.Key, client ?? DisabledHub.Instance);
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
                _options.DiagnosticLogger?.LogDebug("Disposing scope.");

                var previousScopeKey = _snapshot[_snapshot.Length - 1].Key;
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
            _options.DiagnosticLogger?.LogDebug($"Disposing {nameof(SentryScopeManager)}.");
            _asyncLocalScope.Value = null;
        }
    }
}
