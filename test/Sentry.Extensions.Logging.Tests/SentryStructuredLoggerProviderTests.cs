#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class SentryStructuredLoggerProviderTests
{
    private class Fixture
    {
        public IOptions<SentryLoggingOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }
        public SdkVersion Sdk { get; }

        public Fixture()
        {
            var loggingOptions = new SentryLoggingOptions();
            loggingOptions.Experimental.EnableLogs = true;

            Options = Microsoft.Extensions.Options.Options.Create(loggingOptions);
            Hub = Substitute.For<IHub>();
            Clock = new MockClock();
            Sdk = new SdkVersion
            {
                Name = "SDK Name",
                Version = "SDK Version",
            };
        }

        public SentryStructuredLoggerProvider GetSut()
        {
            return new SentryStructuredLoggerProvider(Options.Value, Hub, Clock, Sdk);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Ctor_DependencyInjection_CanCreate()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ILoggerProvider, SentryStructuredLoggerProvider>()
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryStructuredLoggerProviderTests>>();

        logger.Should().BeOfType<Logger<SentryStructuredLoggerProviderTests>>();
    }

    [Fact]
    public void CreateLogger_()
    {
        var provider = _fixture.GetSut();

        var logger = provider.CreateLogger("CategoryName");

        logger.Should().BeOfType<SentryStructuredLogger>();
    }

    [Fact]
    public void Dispose_NoOp()
    {
        var provider = _fixture.GetSut();

        provider.Dispose();

        provider.Dispose();
    }
}
