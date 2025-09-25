#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.AspNetCore.Tests;

public class SentryAspNetCoreStructuredLoggerProviderTests
{
    private class Fixture
    {
        public IOptions<SentryAspNetCoreOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }
        public SdkVersion Sdk { get; }

        public Fixture()
        {
            var loggingOptions = new SentryAspNetCoreOptions();
            loggingOptions.Experimental.EnableLogs = true;

            Options = Microsoft.Extensions.Options.Options.Create(loggingOptions);
            Hub = Substitute.For<IHub>();
            Clock = new MockClock();
            Sdk = new SdkVersion
            {
                Name = "SDK Name",
                Version = "SDK Version",
            };

            Hub.IsEnabled.Returns(true);
        }

        public SentryAspNetCoreStructuredLoggerProvider GetSut()
        {
            return new SentryAspNetCoreStructuredLoggerProvider(Options.Value, Hub, Clock, Sdk);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Ctor_DependencyInjection_CanCreate()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ILoggerProvider, SentryAspNetCoreStructuredLoggerProvider>()
            .AddSingleton(_fixture.Options)
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryAspNetCoreStructuredLoggerProviderTests>>();

        logger.Should().BeOfType<Logger<SentryAspNetCoreStructuredLoggerProviderTests>>();
    }

    [Fact]
    public void CreateLogger_OfType()
    {
        var provider = _fixture.GetSut();

        var logger = provider.CreateLogger("CategoryName");

        logger.Should().BeOfType<Sentry.Extensions.Logging.SentryStructuredLogger>();
    }

    [Fact]
    public void CreateLogger_DependencyInjection_CanLog()
    {
        SentryLog? capturedLog = null;
        _fixture.Hub.Logger.Returns(Substitute.For<SentryStructuredLogger>());
        _fixture.Hub.Logger.CaptureLog(Arg.Do<SentryLog>(log => capturedLog = log));

        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ILoggerProvider, SentryAspNetCoreStructuredLoggerProvider>()
            .AddSingleton(_fixture.Options)
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryAspNetCoreStructuredLoggerProviderTests>>();
        logger.LogInformation("message");

        Assert.NotNull(capturedLog);
        capturedLog.TryGetAttribute("category.name", out object? categoryName).Should().BeTrue();
        categoryName.Should().Be(typeof(SentryAspNetCoreStructuredLoggerProviderTests).FullName);

        capturedLog.TryGetAttribute("sentry.sdk.name", out object? name).Should().BeTrue();
        name.Should().Be(Constants.SdkName);

        capturedLog.TryGetAttribute("sentry.sdk.version", out object? version).Should().BeTrue();
        version.Should().Be(SentryMiddleware.NameAndVersion.Version);

        capturedLog.TryGetAttribute("sentry.origin", out object? origin).Should().BeTrue();
        origin.Should().Be("auto");
    }

    [Fact]
    public void Dispose_NoOp()
    {
        var provider = _fixture.GetSut();

        provider.Dispose();

        provider.Dispose();
    }
}
