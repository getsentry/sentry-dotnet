using System;
using System.Collections.Immutable;
using System.Diagnostics;
#if LACKS_ASYNCLOCAL
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Messaging;
#endif
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class SentryScopeManager : IInternalScopeManager, IDisposable
    {
        private readonly SentryOptions _options;

#if LACKS_ASYNCLOCAL
        static readonly string DataSlotName = typeof(SentryScopeManager).FullName + "@" + Guid.NewGuid();

        private ImmutableStack<(Scope, ISentryClient)> LocalScope
        {
            get
            {
                var objectHandle = CallContext.LogicalGetData(DataSlotName) as ObjectHandle;

                return objectHandle?.Unwrap() as ImmutableStack<(Scope, ISentryClient)>;
            }
            set
            {
                if (CallContext.LogicalGetData(DataSlotName) is IDisposable oldHandle)
                {
                    oldHandle.Dispose();
                }

                CallContext.LogicalSetData(DataSlotName, new DisposableObjectHandle(value));
            }
        }

        private sealed class DisposableObjectHandle : ObjectHandle, IDisposable
        {
            private static readonly ISponsor LifeTimeSponsor = new ClientSponsor();

            public DisposableObjectHandle(object o) : base(o) { }

            public override object InitializeLifetimeService()
            {
                var lease = base.InitializeLifetimeService() as ILease;
                lease?.Register(LifeTimeSponsor);
                return lease;
            }

            public void Dispose()
            {
                if (GetLifetimeService() is ILease lease)
                {
                    lease.Unregister(LifeTimeSponsor);
                }
            }
        }
#else
        private readonly AsyncLocal<ImmutableStack<(Scope, ISentryClient)>> _asyncLocalScope = new AsyncLocal<ImmutableStack<(Scope, ISentryClient)>>();
        private ImmutableStack<(Scope, ISentryClient)> LocalScope
        {
            get => _asyncLocalScope.Value;
            set => _asyncLocalScope.Value = value;
        }
#endif

        internal ImmutableStack<(Scope scope, ISentryClient client)> ScopeAndClientStack
        {
            get => LocalScope ?? (LocalScope = NewStack());
            set => LocalScope = value;
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
            return configureScope?.Invoke(scope.Scope) ??
#if NET45
                   Task.FromResult(null as object);
#else
                   Task.CompletedTask;
#endif
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
            LocalScope = null;
        }
    }
}
