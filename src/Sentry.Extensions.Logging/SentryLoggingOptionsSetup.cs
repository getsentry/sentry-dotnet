using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging
{
    internal class SentryLoggingOptionsSetup : ConfigureFromConfigurationOptions<SentryLoggingOptions>
    {
        public SentryLoggingOptionsSetup(
            ILoggerProviderConfiguration<SentryLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        { }
    }
}
