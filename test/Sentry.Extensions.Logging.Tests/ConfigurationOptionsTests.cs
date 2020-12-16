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
                _ = Builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"));
            }

            public IServiceProvider GetSut()
            {
                var configuration = Builder.Build();
                var services = new ServiceCollection();
                _ = services.AddLogging(builder => builder.AddConfiguration(configuration).AddSentry());
                return services.BuildServiceProvider();
            }
        }

        private readonly Fixture _fixture = new();

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

            _ = _fixture.Builder.AddInMemoryCollection(dict);

            var provider = _fixture.GetSut();
            var sentryLoggingOptions = provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

            Assert.Equal(150, sentryLoggingOptions.MaxBreadcrumbs);
            Assert.Equal("e386dfd", sentryLoggingOptions.Release);
            Assert.Equal("https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537", sentryLoggingOptions.Dsn);
        }

        [Fact]
        public void SentryOptions_DefaultTags_ValuesApplied()
        {
            const string expectedKey = "expected_key";
            const string expectedValue = "expected value";
            var dict = new Dictionary<string, string>
            {
                {"Sentry:DefaultTags:" + expectedKey, expectedValue},
            };

            _ = _fixture.Builder.AddInMemoryCollection(dict);

            var provider = _fixture.GetSut();
            var sentryLoggingOptions = provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

            Assert.Equal(expectedValue, sentryLoggingOptions.DefaultTags[expectedKey]);
        }

        [Fact]
        public void SentryLoggerProvider_ResolvedFromILoggerProvider()
        {
            var provider = _fixture.GetSut();
            _ = Assert.Single(provider.GetServices<ILoggerProvider>().OfType<SentryLoggerProvider>());
        }
    }
}
