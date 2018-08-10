using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging
{
    public class SentryLoggingOptionsSetup : IConfigureOptions<SentryLoggingOptions>
    {
        private readonly IEnumerable<IConfigureOptions<SentryOptions>> _configures;
        private readonly SentryLoggingConfigurationOptions _options;

        public SentryLoggingOptionsSetup(IEnumerable<IConfigureOptions<SentryOptions>> configures, IOptions<SentryLoggingConfigurationOptions> options)
        {
            _configures = configures;
            _options = options.Value;
        }

        public void Configure(SentryLoggingOptions options)
        {
            options.InitializeSdk = _options.InitializeSdk;
            options.MinimumBreadcrumbLevel = _options.MinimumBreadcrumbLevel;
            options.MinimumEventLevel = _options.MinimumEventLevel;

            options.Init(i =>
            {
                i.Dsn = new Dsn(_options.Dsn);
                i.Environment = _options.Environment;
                i.MaxBreadcrumbs = _options.MaxBreadcrumbs;
                i.Release = _options.Release;
                i.SampleRate = _options.SampleRate;
            });

            foreach (var configure in _configures)
                options.Init(configure.Configure);
        }
    }
}
