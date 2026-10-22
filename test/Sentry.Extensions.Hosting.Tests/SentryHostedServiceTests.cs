using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Hosting.Tests;

[Collection(nameof(SentrySdkCollection))]
public sealed class SentryHostedServiceTests : IDisposable
{
    private readonly List<SentryEvent> _events = new();

    public void Dispose() => SentrySdk.Close();

    private void Capture(SentryGenericHostOptions options)
    {
        options.FakeSettings();
        options.Dsn = ValidDsn;
        options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
        options.AutoSessionTracking = false;
        options.InitNativeSdks = false;
        options.SetBeforeSend((e, _) =>
        {
            _events.Add(e);
            return null;
        });
    }

    private sealed class SdkStateAtStart : IHostedService
    {
        public bool? SdkEnabled { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SdkEnabled = SentrySdk.IsEnabled;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FailsWhileStopping(ILoggerFactory loggerFactory) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            loggerFactory.CreateLogger("MyApp.Worker").LogError("failed while stopping");
            return Task.CompletedTask;
        }
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public async Task StartAsync_WorkerHost_LogsNoWarning(BuilderKind kind)
    {
        var logs = new CapturingLoggerProvider();
        var testHost = new TestHost()
            .UseSentry(Capture)
            .ConfigureServices(services => services.AddSingleton<ILoggerProvider>(logs));
        var originalStandardError = Console.Error;
        using var standardError = new StringWriter();
        Console.SetError(standardError);
        try
        {
            using var host = testHost.Build(kind);
            await host.StartAsync();
            await host.StopAsync();
        }
        finally
        {
            Console.SetError(originalStandardError);
        }

        logs.Entries.Should().NotContain(entry => entry.Category == typeof(SentryHostedService).FullName);
        standardError.ToString().Should().NotContain("Sentry:");
    }

    private static void RemoveSdkInitializer(IServiceCollection services)
        => services.Remove(services.Single(d => d.ImplementationType == typeof(SentrySdkInitializer)));

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public async Task StartAsync_SdkNotStartedDuringBuild_SdkRunsBeforeAppHostedServiceStarts(BuilderKind kind)
    {
        var appService = new SdkStateAtStart();
        var testHost = new TestHost()
            .ConfigureServices(services => services.AddSingleton<IHostedService>(appService))
            .UseSentry(Capture)
            .ClearLoggerProviders()
            .ConfigureServices(RemoveSdkInitializer);
        using var host = testHost.Build(kind);
        SentrySdk.IsEnabled.Should().BeFalse();

        await host.StartAsync();

        appService.SdkEnabled.Should().BeTrue();
        await host.StopAsync();
    }

#if NET8_0_OR_GREATER
    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public async Task StartAsync_SdkNotStartedDuringBuildAndServicesStartConcurrently_SdkRunsBeforeAppHostedServiceStarts(BuilderKind kind)
    {
        var appService = new SdkStateAtStart();
        var testHost = new TestHost()
            .ConfigureServices(services =>
            {
                services.Configure<HostOptions>(options => options.ServicesStartConcurrently = true);
                services.AddSingleton<IHostedService>(appService);
            })
            .UseSentry(Capture)
            .ClearLoggerProviders()
            .ConfigureServices(RemoveSdkInitializer);
        using var host = testHost.Build(kind);
        SentrySdk.IsEnabled.Should().BeFalse();

        await host.StartAsync();

        appService.SdkEnabled.Should().BeTrue();
        await host.StopAsync();
    }
#endif

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public async Task StopAsync_ErrorLoggedWhileAppStops_CapturedBeforeSdkCloses(BuilderKind kind)
    {
        var testHost = new TestHost()
            .ConfigureServices(services => services.AddHostedService<FailsWhileStopping>())
            .UseSentry(Capture);
        using var host = testHost.Build(kind);
        await host.StartAsync();

        await host.StopAsync();

        _events.Should().ContainSingle(e => e.Message!.Formatted == "failed while stopping");
        SentrySdk.IsEnabled.Should().BeFalse();
    }
}
