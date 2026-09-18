using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Tests;

public class WebAssemblyHostBuilderExtensionsTests : IDisposable
{
    private readonly List<SentryEvent> _events = new();

    public void Dispose() => SentrySdk.Close();

    private ServiceProvider GetSut(Action<SentryBlazorOptions> configureOptions)
    {
        var services = new ServiceCollection();
        services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        services.AddLogging(logging => logging.AddSentryBlazor(o =>
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
