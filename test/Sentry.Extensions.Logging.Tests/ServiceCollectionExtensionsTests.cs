using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

namespace Sentry.Extensions.Logging.Tests;

[Collection(nameof(SentrySdkCollection))]
public sealed class ServiceCollectionExtensionsTests : IDisposable
{
    private class TestHostOptions : SentryHostOptions;

    private readonly IBackgroundWorker _worker = Substitute.For<IBackgroundWorker>();

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

    private ServiceProvider BuildServiceProvider(bool hostInitializesSdk)
    {
        var services = new ServiceCollection();
        if (hostInitializesSdk)
        {
            services.Configure<TestHostOptions>(o =>
            {
                o.Dsn = ValidDsn;
                o.BackgroundWorker = _worker;
                o.InitNativeSdks = false;
            });
            services.AddSentry<TestHostOptions>(initializeSdk: true);
        }
        else
        {
            SentrySdk.Init(o =>
            {
                o.Dsn = ValidDsn;
                o.BackgroundWorker = _worker;
                o.InitNativeSdks = false;
            });
        }
        services.AddLogging(builder => builder.AddSentry());
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddSentry_SdkHubReplacedAfterResolution_ResolvedHubUsesNewHub(bool hostInitializesSdk)
    {
        using var provider = BuildServiceProvider(hostInitializesSdk);
        var hub = provider.GetRequiredService<IHub>();
        var client = provider.GetRequiredService<ISentryClient>();

        var newHub = Substitute.For<IHub>();
        using var _ = SentrySdk.UseHub(newHub);

        var hubEvent = new SentryEvent();
        var clientEvent = new SentryEvent();
        hub.CaptureEvent(hubEvent);
        client.CaptureEvent(clientEvent);

        newHub.Received(1).CaptureEvent(hubEvent);
        newHub.Received(1).CaptureEvent(clientEvent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddSentry_ServiceProviderDisposed_SdkHubStillCaptures(bool hostInitializesSdk)
    {
        var provider = BuildServiceProvider(hostInitializesSdk);
        _ = provider.GetRequiredService<IHub>();
        _ = provider.GetRequiredService<ISentryClient>();
        _ = provider.GetRequiredService<ILoggerFactory>();

        provider.Dispose();
        SentrySdk.CaptureMessage("after dispose");

        _worker.Received(1).EnqueueEnvelope(Arg.Is<Envelope>(e =>
            e.Items
                .Select(i => i.Payload).OfType<JsonSerializable>()
                .Select(i => i.Source).OfType<SentryEvent>()
                .Any(evt => evt.Message!.Message == "after dispose")));
    }
}
