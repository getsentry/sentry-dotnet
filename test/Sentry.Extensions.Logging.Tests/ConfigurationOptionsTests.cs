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

        using (new AssertionScope())
        {
            sentryLoggingOptions.MinimumBreadcrumbLevel.Should().Be(LogLevel.Warning);
            sentryLoggingOptions.MinimumEventLevel.Should().Be(LogLevel.Critical);
        }
    }

    [Fact]
    public void SentryLoggerProvider_ResolvedFromILoggerProvider()
    {
        var provider = _fixture.GetSut();
        _ = Assert.Single(provider.GetServices<ILoggerProvider>().OfType<SentryLoggerProvider>());
    }
}
