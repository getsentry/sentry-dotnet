using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Tests;

public class WebAssemblyHostBuilderExtensionsTests : IDisposable
{
    private readonly List<SentryEvent> _events = new();

    public void Dispose() => SentrySdk.Close();

    private ServiceProvider GetSut(
        Action<SentryBlazorOptions> configureOptions,
        params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => s.Value))
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        services.AddLogging(logging => logging.AddSentryBlazor(configuration, o =>
        {
            o.Dsn = ValidDsn;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.AutoSessionTracking = false;
            o.SetBeforeSend((e, _) =>
            {
                _events.Add(e);
                return null;
            });
            configureOptions(o);
        }));
        return services.BuildServiceProvider();
    }

    private SentryBlazorOptions GetOptions(
        Action<SentryBlazorOptions> configureOptions,
        params (string Key, string Value)[] settings)
    {
        using var provider = GetSut(configureOptions, settings);
        return provider.GetRequiredService<IOptions<SentryBlazorOptions>>().Value;
    }

    [Fact]
    public void AddSentryBlazor_Configuration_DoesNotOverridePlatformDefaults()
    {
        var options = GetOptions(_ => { },
            ("Sentry:DetectStartupTime", nameof(StartupTimeDetectionMode.Best)),
            ("Sentry:RequestBodyCompressionLevel", nameof(CompressionLevel.Optimal)),
            ("Sentry:IsGlobalModeEnabled", "false"));

        Assert.Equal(StartupTimeDetectionMode.Fast, options.DetectStartupTime);
        Assert.Equal(CompressionLevel.NoCompression, options.RequestBodyCompressionLevel);
        Assert.True(options.IsGlobalModeEnabled);
    }

    [Fact]
    public void AddSentryBlazor_Configuration_AppliedAndOverriddenByCallback()
    {
        var options = GetOptions(o => o.Release = "from-code",
            ("Sentry:Release", "from-configuration"),
            ("Sentry:Environment", "from-configuration"));

        Assert.Equal("from-code", options.Release);
        Assert.Equal("from-configuration", options.Environment);
    }

    [Fact]
    public void UseSentry_CapturedEvent_HasBlazorWebAssemblySdk()
    {
        using var provider = GetSut(_ => { });
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("test_category");

        logger.LogError("message");

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(Constants.SdkName);
    }

    [Fact]
    public void UseSentry_MinimumEventLevel_AppliesToLogger()
    {
        using var provider = GetSut(o => o.MinimumEventLevel = LogLevel.Critical);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("test_category");

        logger.LogError("below the configured level");
        logger.LogCritical("at the configured level");

        _events.Should().ContainSingle().Which.Message!.Message.Should().Be("at the configured level");
    }
}
