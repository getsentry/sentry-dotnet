using System;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    internal class SentryLoggerProvider : ILoggerProvider
    {
        private readonly SentryLoggingOptions _options;
        private IDisposable _scope;

        public SentryLoggerProvider(SentryLoggingOptions options)
        {
            _options = options;

            _scope = SentryCore.PushScope();
            SentryCore.ConfigureScope(p => p.Sdk.Integrations.Add(Constants.IntegrationName));
        }

        public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options);

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}
