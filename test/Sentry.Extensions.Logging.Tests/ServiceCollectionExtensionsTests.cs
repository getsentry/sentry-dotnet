using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

[Collection(nameof(SentrySdkCollection))]
public sealed class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly IBackgroundWorker _worker = Substitute.For<IBackgroundWorker>();

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSentry(o =>
        {
            o.Dsn = ValidDsn;
            o.BackgroundWorker = _worker;
            o.InitNativeSdks = false;
        }));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddSentry_SdkHubReplacedAfterResolution_ResolvedHubUsesNewHub()
    {
        using var provider = BuildServiceProvider();
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

    [Fact]
    public void AddSentry_ServiceProviderDisposed_SdkHubStillCaptures()
    {
        var provider = BuildServiceProvider();
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

    public void Dispose() => SentrySdk.Close();
}
