using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;

namespace Sentry.Extensions.Logging
{
    public class SentryLoggerProvider : ILoggerProvider
    {
        private readonly SentryLoggingOptions _options;
        private IDisposable _scope;
        private IDisposable _sdk;

        public SentryLoggerProvider(SentryLoggingOptions options)
            : this(HubAdapter.Instance, options)
        { }

        internal SentryLoggerProvider(
            ISentryScopeManager scopeManager,
            SentryLoggingOptions options)
        {
            Debug.Assert(options != null);
            Debug.Assert(scopeManager != null);

            _options = options;

            // SDK is being initialized through this integration
            // Lifetime is owned by this instance:
            if (_options.InitializeSdk)
            {
                _sdk = SentryCore.Init(_options.ConfigureOptions);
            }

            // Creates a scope so that Integration added below can be dropped when the logger is disposed
            _scope = scopeManager.PushScope();

            // TODO: SDK interface not accepting 'Integrations'
            // scopeManager.ConfigureScope(s => s.Sdk.AddIntegration(Constants.IntegrationName));
        }

        public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options);

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
            _sdk?.Dispose();
            _sdk = null;
        }
    }
}
