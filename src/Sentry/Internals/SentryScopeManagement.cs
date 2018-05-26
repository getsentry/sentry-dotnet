using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal class SentryScopeManagement : IInternalScopeManagement
    {
        private readonly AsyncLocal<ImmutableStack<Scope>> _asyncLocalScope = new AsyncLocal<ImmutableStack<Scope>>();

        internal ImmutableStack<Scope> ScopeStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = ImmutableStack.Create(new Scope()));
            set => _asyncLocalScope.Value = value;
        }

        public Scope GetCurrent() => ScopeStack.Peek();

        public void ConfigureScope(Action<Scope> configureScope)
        {
            var scope = GetCurrent();
            configureScope?.Invoke(scope);
        }

        // TODO: Microsoft.Extensions.Logging calls its equivalent method: BeginScope()
        public IDisposable PushScope()
        {
            var currentScopeStack = ScopeStack;
            var clonedScope = currentScopeStack.Peek().Clone();
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
