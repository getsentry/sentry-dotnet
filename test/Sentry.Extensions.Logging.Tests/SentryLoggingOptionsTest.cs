using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggingOptionsTest
    {
        private readonly SentryLoggingOptions _options;
        public SentryLoggingOptionsTest(IOptions<SentryLoggingOptions> options) => _options = options.Value;

        [Fact]
        public void SentryLoggingOptions()
        {
            Assert.False(_options.InitializeSdk);
            Assert.Equal(LogLevel.Warning, _options.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Critical, _options.MinimumEventLevel);
        }

        [Fact]
        public void SentryOptions()
        {
            Assert.Single(_options.ConfigureOptionsActions);

            var options = new SentryOptions();

            _options.ConfigureOptionsActions[0](options);

            Assert.Equal(150, options.MaxBreadcrumbs);
            Assert.Equal("e386dfd", options.Release);
            Assert.Equal("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141", options.Dsn.ToString());
        }
    }
}
