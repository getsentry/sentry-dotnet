using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class SentryScopeManagement : IInternalScopeManagement
    {
        private readonly AsyncLocal<ImmutableStack<(Scope, ISentryClient)>> _asyncLocalScope = new AsyncLocal<ImmutableStack<(Scope, ISentryClient)>>();

        internal ImmutableStack<(Scope scope, ISentryClient client)> ScopeAndClientStack
        {
            get => _asyncLocalScope.Value;
            set => _asyncLocalScope.Value = value;
        }

        public SentryScopeManagement(
            IScopeOptions options,
            ISentryClient rootClient)
        {
            Debug.Assert(rootClient != null);
            _asyncLocalScope.Value = ImmutableStack.Create((new Scope(options), rootClient));
        }

        public (Scope Scope, ISentryClient Client) GetCurrent() => ScopeAndClientStack.Peek();

        public void ConfigureScope(Action<Scope> configureScope)
        {
            var scope = GetCurrent();
            configureScope?.Invoke(scope.Scope);
        }

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        {
            var scope = GetCurrent();
            return configureScope?.Invoke(scope.Scope) ?? Task.CompletedTask;
        }

        public IDisposable PushScope() => PushScope<object>(null);

        public IDisposable PushScope<TState>(TState state)
        {
            var currentScopeAndClientStack = ScopeAndClientStack;
            var (scope, client) = currentScopeAndClientStack.Peek();
            var clonedScope = scope.Clone(state);
            var scopeSnapshot = new ScopeSnapshot(currentScopeAndClientStack, this);
            ScopeAndClientStack = currentScopeAndClientStack.Push((clonedScope, client));

            return scopeSnapshot;
        }

        public void BindClient(ISentryClient client)
        {
            var currentScopeAndClientStack = ScopeAndClientStack;
            currentScopeAndClientStack = currentScopeAndClientStack.Pop(out var top);
            currentScopeAndClientStack = currentScopeAndClientStack.Push((top.scope, client ?? DisabledSentryClient.Instance));
            ScopeAndClientStack = currentScopeAndClientStack;
        }

        private class ScopeSnapshot : IDisposable
        {
            private readonly ImmutableStack<(Scope scope, ISentryClient client)> _snapshot;
            private readonly SentryScopeManagement _scopeManagement;

            public ScopeSnapshot(ImmutableStack<(Scope, ISentryClient)> snapshot, SentryScopeManagement scopeManagement)
            {
                Debug.Assert(snapshot != null);
                Debug.Assert(scopeManagement != null);
                _snapshot = snapshot;
                _scopeManagement = scopeManagement;
            }

            public void Dispose()
            {
                // Only reset the parent if this is still the current scope
                foreach (var (scope, _) in _scopeManagement.ScopeAndClientStack)
                {
                    if (ReferenceEquals(scope, _snapshot.Peek().scope))
                    {
                        _scopeManagement.ScopeAndClientStack = _snapshot;
                        break;
                    }
                }
            }
        }
    }
}
