using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal sealed class SentryScopeManager : IInternalScopeManager, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly AsyncLocal<Stack<KeyValuePair<Scope, ISentryClient>>> _asyncLocalScope = new AsyncLocal<Stack<KeyValuePair<Scope, ISentryClient>>>();

        internal Stack<KeyValuePair<Scope, ISentryClient>> ScopeAndClientStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = NewStack());
            set => _asyncLocalScope.Value = value;
        }

        private Func<Stack<KeyValuePair<Scope, ISentryClient>>> NewStack { get; }

        public SentryScopeManager(
            SentryOptions options,
            ISentryClient rootClient)
        {
            Debug.Assert(rootClient != null);
            _options = options;
            NewStack = () => new Stack<KeyValuePair<Scope, ISentryClient>>(new [] { new KeyValuePair<Scope, ISentryClient>(new Scope(options), rootClient) });
        }

        public KeyValuePair<Scope, ISentryClient> GetCurrent() => ScopeAndClientStack.Peek();

        public void ConfigureScope(Action<Scope> configureScope)
        {
            _options?.DiagnosticLogger?.LogDebug("Configuring the scope.");
            var scope = GetCurrent();
            configureScope?.Invoke(scope.Key);
        }

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        {
            _options?.DiagnosticLogger?.LogDebug("Configuring the scope asynchronously.");
            var scope = GetCurrent();
            return configureScope?.Invoke(scope.Key) ?? Task.CompletedTask;
        }

        public IDisposable PushScope() => PushScope<object>(null);

        public IDisposable PushScope<TState>(TState state)
        {
            var currentScopeAndClientStack = ScopeAndClientStack;
            var scope = currentScopeAndClientStack.Peek();

            if (scope.Key.Locked)
            {
                // TODO: keep state on current scope?
                _options?.DiagnosticLogger?.LogDebug("Locked scope. No new scope pushed.");
                return DisabledHub.Instance;
            }

            var clonedScope = scope.Key.Clone();

            if (state != null)
            {
                clonedScope.Apply(state);
            }

            var scopeSnapshot = new ScopeSnapshot(_options, currentScopeAndClientStack, this);

            _options?.DiagnosticLogger?.LogDebug("New scope pushed.");
            var newScopeAndClientStack = new Stack<KeyValuePair<Scope, ISentryClient>>(currentScopeAndClientStack.Count + 1);
            foreach (var item in currentScopeAndClientStack)
            {
                newScopeAndClientStack.Push(item);
            }
            newScopeAndClientStack.Push(new KeyValuePair<Scope, ISentryClient>(clonedScope, scope.Value));

            ScopeAndClientStack = newScopeAndClientStack;
            return scopeSnapshot;
        }

        public void WithScope(Action<Scope> scopeCallback)
        {
            using (PushScope())
            {
                var scope = GetCurrent();
                scopeCallback?.Invoke(scope.Key);
            }
        }

        public void BindClient(ISentryClient client)
        {
            _options?.DiagnosticLogger?.LogDebug("Binding a new client to the current scope.");

            var currentScopeAndClientStack = ScopeAndClientStack;
            var newScopeAndClientStack = new Stack<KeyValuePair<Scope, ISentryClient>>(currentScopeAndClientStack);
            var top = newScopeAndClientStack.Pop();
            newScopeAndClientStack.Push(new KeyValuePair<Scope, ISentryClient>(top.Key, client ?? DisabledHub.Instance));
            ScopeAndClientStack = newScopeAndClientStack;
        }

        private sealed class ScopeSnapshot : IDisposable
        {
            private readonly SentryOptions _options;
            private readonly Stack<KeyValuePair<Scope, ISentryClient>> _snapshot;
            private readonly SentryScopeManager _scopeManager;

            public ScopeSnapshot(
                SentryOptions options,
                Stack<KeyValuePair<Scope, ISentryClient>> snapshot,
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
                foreach (var scope in _scopeManager.ScopeAndClientStack)
                {
                    if (ReferenceEquals(scope.Key, _snapshot.Peek().Key))
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
