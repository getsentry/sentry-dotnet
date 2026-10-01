using Microsoft.Extensions.DependencyInjection;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

namespace Sentry.Extensions.Logging.Tests;

public class ServiceCollectionExtensionsTests : IDisposable
{
    private class TestHostOptions : SentryHostOptions;

    public void Dispose() => SentrySdk.Close();

    private static ServiceProvider GetSut(bool initializeSdk, Action<Scope> configureScope)
    {
        var services = new ServiceCollection();
        services.Configure<TestHostOptions>(o =>
        {
            o.Dsn = ValidDsn;
            o.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            o.AutoSessionTracking = false;
            o.InitNativeSdks = false;
            o.ConfigureScope(configureScope);
        });
        services.AddSentry<TestHostOptions>(initializeSdk);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddSentry_HubResolved_InitializesSdkAndAppliesConfigureScope()
    {
        var configured = false;
        using var provider = GetSut(initializeSdk: true, _ => configured = true);

        _ = provider.GetRequiredService<IHub>();

        SentrySdk.IsEnabled.Should().BeTrue();
        configured.Should().BeTrue();
    }

    [Fact]
    public void AddSentry_WithoutInitializeSdk_HubResolved_LeavesSdkAlone()
    {
        SentrySdk.UseHub(DisabledHub.Instance);
        var configured = false;
        using var provider = GetSut(initializeSdk: false, _ => configured = true);

        _ = provider.GetRequiredService<IHub>();

        SentrySdk.IsEnabled.Should().BeFalse();
        configured.Should().BeFalse();
    }
}
