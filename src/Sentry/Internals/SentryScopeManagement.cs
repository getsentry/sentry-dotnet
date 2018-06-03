using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal class SentryScopeManagement : IInternalScopeManagement
    {
        private readonly IScopeOptions _options;
        private readonly AsyncLocal<ImmutableStack<Scope>> _asyncLocalScope = new AsyncLocal<ImmutableStack<Scope>>();

        internal ImmutableStack<Scope> ScopeStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = ImmutableStack.Create(new Scope(_options)));
            set => _asyncLocalScope.Value = value;
        }

        public SentryScopeManagement(IScopeOptions options) => _options = options;

        public Scope GetCurrent() => ScopeStack.Peek();

        public void ConfigureScope(Action<Scope> configureScope)
        {
            var scope = GetCurrent();
            configureScope?.Invoke(scope);
        }

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        {
            var scope = GetCurrent();
            return configureScope?.Invoke(scope) ?? Task.CompletedTask;
        }

        public IDisposable PushScope() => PushScope<object>(null);

        public IDisposable PushScope<TState>(TState state)
        {
            var currentScopeStack = ScopeStack;
            var clonedScope = currentScopeStack.Peek().Clone(state);
            var scopeSnapshot = new ScopeSnapshot(currentScopeStack, this);
            ScopeStack = currentScopeStack.Push(clonedScope);

            return scopeSnapshot;
        }

        private class ScopeSnapshot : IDisposable
        {
            private readonly ImmutableStack<Scope> _snapshot;
            private readonly SentryScopeManagement _scopeManagement;

            public ScopeSnapshot(ImmutableStack<Scope> snapshot, SentryScopeManagement scopeManagement)
            {
                Debug.Assert(snapshot != null);
                Debug.Assert(scopeManagement != null);
                _snapshot = snapshot;
                _scopeManagement = scopeManagement;
            }

            public void Dispose() => _scopeManagement.ScopeStack = _snapshot;
        }
    }
}
