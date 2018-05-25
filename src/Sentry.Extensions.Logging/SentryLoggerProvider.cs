using Sentry.Extensions.Logging;

namespace Microsoft.Extensions.Logging
{
    internal class SentryLoggerProvider : ILoggerProvider
    {
        private readonly SentryLoggingOptions _options;

        public SentryLoggerProvider(SentryLoggingOptions options) => _options = options;

        public ILogger CreateLogger(string categoryName)
        {
            return new SentryLogger(categoryName, _options);
        }

        public void Dispose()
        {
            // no op, integration doesn't manage the lifeitme of the client
        }
    }
}
