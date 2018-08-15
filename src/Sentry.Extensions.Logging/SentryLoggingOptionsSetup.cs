using System.Collections.Generic;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging
{
    public class SentryLoggingOptionsSetup : ConfigureFromConfigurationOptions<SentryLoggingOptions>
    {
        private readonly IEnumerable<IConfigureOptions<SentryOptions>> _configures;

        public SentryLoggingOptionsSetup(
            IEnumerable<IConfigureOptions<SentryOptions>> configures,
            ILoggerProviderConfiguration<SentryLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
            => _configures = configures;

        public override void Configure(SentryLoggingOptions options)
        {
            base.Configure(options);

            if (options.InitializeSdk && options.Dsn != null && !Dsn.IsDisabled(options.Dsn))
            {
                options.Init(i =>
                {
                    i.Dsn = new Dsn(options.Dsn);
                    i.Environment = options.Environment;
                    i.MaxBreadcrumbs = options.MaxBreadcrumbs;
                    i.Release = options.Release;
                    i.SampleRate = options.SampleRate;
                });

                foreach (var configure in _configures)
                {
                    options.Init(configure.Configure);
                }
            }
        }
    }
}
