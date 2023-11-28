using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class ConfigurationOptionsTests
{
    private class Fixture
    {
        public ConfigurationBuilder Builder { get; set; }

        public Fixture()
        {
            Builder = new ConfigurationBuilder();
#if ANDROID
            var stream = Application.Context.Assets?.Open("appsettings.json");
            if (stream != null)
            {
                Builder.AddJsonStream(stream);
            }
#else
            Builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"));
#endif
        }

        public IServiceProvider GetSut()
        {
            var configuration = Builder.Build();
            var services = new ServiceCollection();
            _ = services.AddLogging(builder => builder.AddConfiguration(configuration).AddSentry(o =>
            {
                o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
                o.InitNativeSdks = false;
                o.AutoSessionTracking = false;
            }));
            return services.BuildServiceProvider();
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void SentryLoggingOptions_ValuesFromAppSettings()
    {
        var provider = _fixture.GetSut();
        var sentryLoggingOptions = provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

        using (new AssertionScope())
        {
            sentryLoggingOptions.InitializeSdk.Should().BeFalse();
            sentryLoggingOptions.MinimumBreadcrumbLevel.Should().Be(LogLevel.Warning);
            sentryLoggingOptions.MinimumEventLevel.Should().Be(LogLevel.Critical);
        }
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
        Assert.Equal(ValidDsn, sentryLoggingOptions.Dsn);
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

        sentryLoggingOptions.DefaultTags.Should().ContainKey(expectedKey);
        sentryLoggingOptions.DefaultTags[expectedKey].Should().Be(expectedValue);
    }

    [Fact]
    public void SentryLoggerProvider_ResolvedFromILoggerProvider()
    {
        var provider = _fixture.GetSut();
        _ = Assert.Single(provider.GetServices<ILoggerProvider>().OfType<SentryLoggerProvider>());
    }
}
