#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests.Internal;

public class SentryMauiStructuredLoggerProviderTests
{
    private class Fixture
    {
        public IOptions<SentryMauiOptions> Options { get; }
        public IHub Hub { get; }
        public MockClock Clock { get; }
        public SdkVersion Sdk { get; }

        public Fixture()
        {
            var loggingOptions = new SentryMauiOptions();
            loggingOptions.EnableLogs = true;

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

        public SentryMauiStructuredLoggerProvider GetSut()
        {
            return new SentryMauiStructuredLoggerProvider(Options.Value, Hub, Clock, Sdk);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Type_CustomAttributes_HasProviderAliasAttribute()
    {
        var type = typeof(SentryMauiStructuredLoggerProvider);

        type.GetCustomAttributes<ProviderAliasAttribute>().Should()
            .ContainSingle().Which
            .Alias.Should().Be("Sentry");
    }

    [Fact]
    public void Ctor_DependencyInjection_CanCreate()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ILoggerProvider, SentryMauiStructuredLoggerProvider>()
            .AddSingleton(_fixture.Options)
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryMauiStructuredLoggerProviderTests>>();

        logger.Should().BeOfType<Logger<SentryMauiStructuredLoggerProviderTests>>();
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
            .AddSingleton<ILoggerProvider, SentryMauiStructuredLoggerProvider>()
            .AddSingleton(_fixture.Options)
            .AddSingleton(_fixture.Hub)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SentryMauiStructuredLoggerProviderTests>>();
        logger.LogInformation("message");

        Assert.NotNull(capturedLog);
        capturedLog.TryGetAttribute("category.name", out object? categoryName).Should().BeTrue();
        categoryName.Should().Be(typeof(SentryMauiStructuredLoggerProviderTests).FullName);

        capturedLog.TryGetAttribute("sentry.sdk.name", out object? name).Should().BeTrue();
        name.Should().Be(Sentry.Maui.Internal.Constants.SdkName);

        capturedLog.TryGetAttribute("sentry.sdk.version", out object? version).Should().BeTrue();
        version.Should().Be(Sentry.Maui.Internal.Constants.SdkVersion);

        capturedLog.TryGetAttribute("sentry.origin", out object? origin).Should().BeTrue();
        origin.Should().Be("auto.log.extensions_logging");
    }

    [Fact]
    public void Dispose_NoOp()
    {
        var provider = _fixture.GetSut();

        provider.Dispose();

        provider.Dispose();
    }
}
