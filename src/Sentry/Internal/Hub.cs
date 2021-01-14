using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class Hub : IHub, IDisposable
    {
        private readonly ISentryClient _ownedClient;
        private readonly SentryOptions _options;
        private readonly ISdkIntegration[]? _integrations;
        private readonly IDisposable _rootScope;

        internal SentryScopeManager ScopeManager { get; }

        public bool IsEnabled => true;

        internal Hub(ISentryClient client, SentryOptions options)
        {
            _ownedClient = client;

            _options = options;

            if (options.Dsn is null)
            {
                var dsn = DsnLocator.FindDsnStringOrDisable();

                if (Dsn.TryParse(dsn) is null)
                {
                    const string msg = "Attempt to instantiate a Hub without a DSN.";
                    options.DiagnosticLogger?.LogFatal(msg);
                    throw new InvalidOperationException(msg);
                }

                options.Dsn = dsn;
            }

            options.DiagnosticLogger?.LogDebug("Initializing Hub for Dsn: '{0}'.", options.Dsn);

            ScopeManager = new SentryScopeManager(options, _ownedClient);

            _integrations = options.Integrations;

            if (_integrations?.Length > 0)
            {
                foreach (var integration in _integrations)
                {
                    options.DiagnosticLogger?.LogDebug("Registering integration: '{0}'.", integration.GetType().Name);
                    integration.Register(this, options);
                }
            }

            // Push the first scope so the async local starts from here
            _rootScope = PushScope();
        }

        public Hub(SentryOptions options)
            : this(new SentryClient(options), options)
        {
        }

        public void ConfigureScope(Action<Scope> configureScope)
        {
            try
            {
                ScopeManager.ConfigureScope(configureScope);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to ConfigureScope", e);
            }
        }

        public async Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        {
            try
            {
                await ScopeManager.ConfigureScopeAsync(configureScope).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to ConfigureScopeAsync", e);
            }
        }

        public IDisposable PushScope() => ScopeManager.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManager.PushScope(state);

        public void WithScope(Action<Scope> scopeCallback)
        {
            try
            {
                ScopeManager.WithScope(scopeCallback);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to run callback WithScope", e);
            }
        }

        public void BindClient(ISentryClient client) => ScopeManager.BindClient(client);

        public Transaction CreateTransaction(string name, string operation)
        {
            var trans = new Transaction(this)
            {
                Name = name,
                Operation = operation
            };

            var nameAndVersion = MainSentryEventProcessor.NameAndVersion;
            var protocolPackageName = MainSentryEventProcessor.ProtocolPackageName;

            if (trans.Sdk.Version == null && trans.Sdk.Name == null)
            {
                trans.Sdk.Name = Constants.SdkName;
                trans.Sdk.Version = nameAndVersion.Version;
            }

            if (nameAndVersion.Version != null)
            {
                trans.Sdk.AddPackage(protocolPackageName, nameAndVersion.Version);
            }

            ConfigureScope(scope => scope.Transaction = trans);

            return trans;
        }

        public SentryTraceHeader? GetSentryTrace()
        {
            var (currentScope, _) = ScopeManager.GetCurrent();
            return currentScope.Transaction?.GetTraceHeader();
        }

        public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null)
        {
            try
            {
                var currentScope = ScopeManager.GetCurrent();
                var actualScope = scope ?? currentScope.Key;
                var id = currentScope.Value.CaptureEvent(evt, actualScope);
                actualScope.LastEventId = id;
                return id;
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture event: {0}", e, evt.EventId);
                return SentryId.Empty;
            }
        }

        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
            try
            {
                _ownedClient.CaptureUserFeedback(userFeedback);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture user feedback: {0}", e, userFeedback.EventId);
            }
        }

        public void CaptureTransaction(Transaction transaction)
        {
            try
            {
                _ownedClient.CaptureTransaction(transaction);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture transaction: {0}", e, transaction.SpanId);
            }
        }

        public async Task FlushAsync(TimeSpan timeout)
        {
            try
            {
                var currentScope = ScopeManager.GetCurrent();
                await currentScope.Value.FlushAsync(timeout).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to Flush events", e);
            }
        }

        public void Dispose()
        {
            _options.DiagnosticLogger?.LogInfo("Disposing the Hub.");

            if (_integrations?.Length > 0)
            {
                foreach (var integration in _integrations)
                {
                    if (integration is IInternalSdkIntegration internalIntegration)
                    {
                        internalIntegration.Unregister(this);
                    }
                }
            }

            (_ownedClient as IDisposable)?.Dispose();
            _rootScope.Dispose();
            ScopeManager.Dispose();
        }

        public SentryId LastEventId
        {
            get
            {
                var currentScope = ScopeManager.GetCurrent();
                return currentScope.Key.LastEventId;
            }
        }
    }
}
