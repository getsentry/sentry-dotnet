using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;

namespace Sentry.Extensions.Logging
{
    internal class SentryLoggerProvider : ILoggerProvider
    {
        private readonly SentryLoggingOptions _options;
        private IDisposable _scope;

        public SentryLoggerProvider(SentryLoggingOptions options)
            : this(SentryCoreAdapter.Instance, options)
        { }

        internal SentryLoggerProvider(
            ISentryScopeManagement scopeManagement,
            SentryLoggingOptions options)
        {
            Debug.Assert(options != null);
            Debug.Assert(scopeManagement != null);

            _options = options;
            // Creates a scope so that Integration added below can be dropped when the logger is disposed
            _scope = scopeManagement.PushScope();

            scopeManagement.ConfigureScope(p => p.Sdk.Integrations.Add(Constants.IntegrationName));
        }

        public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options);

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}
