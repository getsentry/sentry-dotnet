using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class ConfigurationOptions
    {
        private static readonly IServiceProvider Provider;

        static ConfigurationOptions()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json")).Build();

            services.AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddSentry());

            Provider = services.BuildServiceProvider();
        }

        [Fact]
        public void SentryLoggingOptionsTest()
        {
            var sentryLoggingOptions = Provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

            Assert.False(sentryLoggingOptions.InitializeSdk);
            Assert.Equal(LogLevel.Warning, sentryLoggingOptions.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Critical, sentryLoggingOptions.MinimumEventLevel);
        }

        [Fact]
        public void SentryOptionsTest()
        {
            var sentryLoggingOptions = Provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

            Assert.Single(sentryLoggingOptions.ConfigureOptionsActions);

            var sentryOptions = new SentryOptions();

            sentryLoggingOptions.ConfigureOptionsActions[0](sentryOptions);

            Assert.Equal(150, sentryOptions.MaxBreadcrumbs);
            Assert.Equal("e386dfd", sentryOptions.Release);
            Assert.Equal("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141", sentryOptions.Dsn.ToString());
        }

        [Fact]
        public void SentryLoggerProviderTest()
        {
            Assert.Single(Provider.GetServices<ILoggerProvider>().OfType<SentryLoggerProvider>());
        }
    }
}
