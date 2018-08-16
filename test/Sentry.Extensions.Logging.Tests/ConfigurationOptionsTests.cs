using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class ConfigurationOptionsTests
    {
        private class Fixture
        {
            public ConfigurationBuilder Builder { get; set; }

            public Fixture()
            {
                Builder = new ConfigurationBuilder();
                Builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"));
            }

            public IServiceProvider GetSut()
            {
                var configuration = Builder.Build();
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddConfiguration(configuration).AddSentry());
                return services.BuildServiceProvider();
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void SentryLoggingOptions_ValuesFromAppSettings()
        {
            var provider = _fixture.GetSut();
            var sentryLoggingOptions = provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

            Assert.False(sentryLoggingOptions.InitializeSdk);
            Assert.Equal(LogLevel.Warning, sentryLoggingOptions.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Critical, sentryLoggingOptions.MinimumEventLevel);
        }

        [Fact]
        public void SentryOptions_InitializeTrue_ValuesAppliedFromLoggingOptions()
        {
            var dict = new Dictionary<string, string>
            {
                {"Sentry:InitializeSdk", "true"},
            };

            _fixture.Builder.AddInMemoryCollection(dict);

            var provider = _fixture.GetSut();
            var sentryLoggingOptions = provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

            Assert.Equal(150, sentryLoggingOptions.MaxBreadcrumbs);
            Assert.Equal("e386dfd", sentryLoggingOptions.Release);
            Assert.Equal("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141", sentryLoggingOptions.Dsn);
        }

        [Fact]
        public void SentryLoggerProvider_ResolvedFromILoggerProvider()
        {
            var provider = _fixture.GetSut();
            Assert.Single(provider.GetServices<ILoggerProvider>().OfType<SentryLoggerProvider>());
        }
    }
}
