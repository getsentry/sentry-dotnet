using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class ConfigurationOptions
    {
        private readonly IEnumerable<ILoggerProvider> _providers;
        private readonly SentryLoggingOptions _options;
        public ConfigurationOptions(IOptions<SentryLoggingOptions> options, IEnumerable<ILoggerProvider> providers)
        {
            _providers = providers;
            _options = options.Value;
        }

        [Fact]
        public void SentryLoggingOptionsTest()
        {
            Assert.False(_options.InitializeSdk);
            Assert.Equal(LogLevel.Warning, _options.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Critical, _options.MinimumEventLevel);
        }

        [Fact]
        public void SentryOptionsTest()
        {
            Assert.Single(_options.ConfigureOptionsActions);

            var options = new SentryOptions();

            _options.ConfigureOptionsActions[0](options);

            Assert.Equal(150, options.MaxBreadcrumbs);
            Assert.Equal("e386dfd", options.Release);
            Assert.Equal("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141", options.Dsn.ToString());
        }

        [Fact]
        public void SentryLoggerProviderTest()
        {
            Assert.Single(_providers.OfType<SentryLoggerProvider>());
        }
    }
}
