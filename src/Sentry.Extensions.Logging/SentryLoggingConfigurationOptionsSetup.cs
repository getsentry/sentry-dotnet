using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging
{
    internal class SentryLoggingConfigurationOptionsSetup : ConfigureFromConfigurationOptions<SentryLoggingConfigurationOptions>
    {
        public SentryLoggingConfigurationOptionsSetup(ILoggerProviderConfiguration<SentryLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        { }
    }
}
