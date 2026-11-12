using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.Extensions.Hosting.Tests;

[Collection(nameof(SentrySdkCollection))]
public sealed class SentryHostBuilderExtensionsTests : IDisposable
{
    private readonly List<SentryEvent> _events = new();
    private readonly List<SentryLog> _logs = new();
    private readonly InMemoryDiagnosticLogger _diagnosticLogger = new();

    public void Dispose() => SentrySdk.Close();

    private void DoNotSend(SentryGenericHostOptions options)
    {
        options.FakeSettings();
        options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
        options.AutoSessionTracking = false;
        options.InitNativeSdks = false;
        options.SetBeforeSend((e, _) =>
        {
            _events.Add(e);
            return null;
        });
        options.SetBeforeSendLog(log =>
        {
            _logs.Add(log);
            return null;
        });
    }

    private void Capture(SentryGenericHostOptions options)
    {
        options.Dsn = ValidDsn;
        options.EnableLogs = true;
        DoNotSend(options);
    }

    private void CaptureDiagnostics(SentryGenericHostOptions options)
    {
        Capture(options);
        options.Debug = true;
        options.DiagnosticLogger = _diagnosticLogger;
    }

    private IEnumerable<InMemoryDiagnosticLogger.Entry> LoggerProvidersRemovedWarnings()
        => _diagnosticLogger.Entries.Where(entry =>
            entry.Level == SentryLevel.Warning && entry.Message == SentryLoggingOptions.HostLoggerProvidersRemoved);

    private static SentryGenericHostOptions ResolveOptions(IHost host)
        => host.Services.GetRequiredService<IOptions<SentryGenericHostOptions>>().Value;

    private static void LogError(IHost host, string message)
        => host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Worker").LogError(message);

    private static void AssertIntegrationRegistered(IHost host)
    {
        host.Services.GetServices<ILoggerProvider>().Should()
            .ContainSingle(provider => provider is SentryLoggerProvider)
            .And.ContainSingle(provider => provider is SentryStructuredLoggerProvider);
        host.Services.GetServices<IHostedService>().Should().ContainSingle(service => service is SentryHostedService);
        host.Services.GetRequiredService<IHub>().IsEnabled.Should().BeTrue();
        host.Services.GetRequiredService<ISentryClient>().IsEnabled.Should().BeTrue();
        ResolveOptions(host).Dsn.Should().Be(ValidDsn);
    }

#if NET8_0_OR_GREATER
    public static TheoryData<string, Action<IHostApplicationBuilder>> ApplicationBuilderOverloads => new()
    {
        { "parameterless", builder => builder.UseSentry() },
        { "dsn", builder => builder.UseSentry(ValidDsn) },
        { "options callback", builder => builder.UseSentry(options => options.Dsn = ValidDsn) },
    };

    [Theory, MemberData(nameof(ApplicationBuilderOverloads))]
    public void UseSentry_ApplicationBuilderOverload_RegistersIntegration(
        string overload, Action<IHostApplicationBuilder> useSentry)
    {
        _ = overload;
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sentry:Dsn"] = ValidDsn });
        builder.Services.Configure<SentryGenericHostOptions>(DoNotSend);

        useSentry(builder);
        using var host = builder.Build();

        AssertIntegrationRegistered(host);
    }
#endif

    public static TheoryData<string, Action<IHostBuilder>> HostBuilderOverloads => new()
    {
        { "parameterless", builder => builder.UseSentry() },
        { "dsn", builder => builder.UseSentry(ValidDsn) },
        { "options callback", builder => builder.UseSentry(options => options.Dsn = ValidDsn) },
        { "context and options callback", builder => builder.UseSentry((_, options) => options.Dsn = ValidDsn) },
    };

    [Theory, MemberData(nameof(HostBuilderOverloads))]
    public void UseSentry_HostBuilderOverload_RegistersIntegration(string overload, Action<IHostBuilder> useSentry)
    {
        _ = overload;
        var builder = new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Sentry:Dsn"] = ValidDsn }))
            .ConfigureServices(services => services.Configure<SentryGenericHostOptions>(DoNotSend));

        useSentry(builder);
        using var host = builder.Build();

        AssertIntegrationRegistered(host);
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_SentryAndLoggingSections_BindsOptions(BuilderKind kind)
    {
        var testHost = new TestHost().UseSentry(DoNotSend);
        testHost.Settings["Sentry:Dsn"] = ValidDsn;
        testHost.Settings["Sentry:SendDefaultPii"] = "true";
        testHost.Settings["Sentry:EnableLogs"] = "true";
        testHost.Settings["Logging:Sentry:MinimumEventLevel"] = "Warning";

        using var host = testHost.Build(kind);

        var options = ResolveOptions(host);
        options.Dsn.Should().Be(ValidDsn);
        options.SendDefaultPii.Should().BeTrue();
        options.EnableLogs.Should().BeTrue();
        options.MinimumEventLevel.Should().Be(LogLevel.Warning);
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_OptionsCallback_RunsAfterConfigurationBinding(BuilderKind kind)
    {
        var testHost = new TestHost().UseSentry(options =>
        {
            Capture(options);
            options.SendDefaultPii = false;
        });
        testHost.Settings["Sentry:SendDefaultPii"] = "true";

        using var host = testHost.Build(kind);

        ResolveOptions(host).SendDefaultPii.Should().BeFalse();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_NoEnvironmentConfigured_UsesHostEnvironment(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }.UseSentry(Capture);

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("staging");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CasingFlagInConfiguration_KeepsHostEnvironmentCasing(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }.UseSentry(Capture);
        testHost.Settings["Sentry:AdjustStandardEnvironmentNameCasing"] = "false";

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("Staging");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CasingFlagInCallback_KeepsHostEnvironmentCasing(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }.UseSentry(options =>
        {
            Capture(options);
            options.AdjustStandardEnvironmentNameCasing = false;
        });

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("Staging");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CasingFlagInLaterConfigure_KeepsHostEnvironmentCasing(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }
            .UseSentry(Capture)
            .ConfigureServices(services => services.Configure<SentryGenericHostOptions>(
                options => options.AdjustStandardEnvironmentNameCasing = false));

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("Staging");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_EnvironmentInCallback_OverridesHostEnvironment(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }.UseSentry(options =>
        {
            Capture(options);
            options.Environment = "from-code";
        });

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("from-code");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_EnvironmentInConfiguration_OverridesHostEnvironment(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }.UseSentry(Capture);
        testHost.Settings["Sentry:Environment"] = "from-config";

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("from-config");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CalledTwice_SendsEachErrorAndLogOnce(BuilderKind kind)
    {
        var testHost = new TestHost().UseSentry(Capture).UseSentry(Capture);
        using var host = testHost.Build(kind);

        LogError(host, "one logged error");

        _events.Should().ContainSingle();
        _logs.Should().ContainSingle(log => log.Message == "one logged error");
        host.Services.GetServices<ILoggerProvider>().Should().ContainSingle(provider => provider is SentryLoggerProvider);
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CalledTwice_AppliesOptionsFromBothCalls(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(options =>
            {
                Capture(options);
                options.SendDefaultPii = true;
                options.MaxBreadcrumbs = 10;
            })
            .UseSentry(options =>
            {
                options.Release = "second-call";
                options.MaxBreadcrumbs = 20;
            });

        using var host = testHost.Build(kind);

        var options = ResolveOptions(host);
        options.SendDefaultPii.Should().BeTrue();
        options.Release.Should().Be("second-call");
        options.MaxBreadcrumbs.Should().Be(20);
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CasingFlagInSecondCall_KeepsHostEnvironmentCasing(BuilderKind kind)
    {
        var testHost = new TestHost { EnvironmentName = "Staging" }
            .UseSentry(Capture)
            .UseSentry(options => options.AdjustStandardEnvironmentNameCasing = false);

        using var host = testHost.Build(kind);

        ResolveOptions(host).Environment.Should().Be("Staging");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void LoggingAddSentry_AfterUseSentry_DoesNotSendErrorsTwice(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(Capture)
            .LoggingAddSentry();
        using var host = testHost.Build(kind);

        LogError(host, "one logged error");

        _events.Should().ContainSingle();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void LoggingAddSentry_BeforeUseSentry_DoesNotSendErrorsTwice(BuilderKind kind)
    {
        var testHost = new TestHost()
            .LoggingAddSentry()
            .UseSentry(Capture);
        using var host = testHost.Build(kind);

        LogError(host, "one logged error");

        _events.Should().ContainSingle();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void LoggingAddSentry_AfterClearProviders_SendsErrorAndLogOnce(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(Capture)
            .ClearLoggerProviders()
            .LoggingAddSentry();
        using var host = testHost.Build(kind);

        LogError(host, "one logged error");

        _events.Should().ContainSingle();
        _logs.Should().ContainSingle();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void LoggingAddSentry_AfterClearProviders_UsesHostOptions(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(options =>
            {
                Capture(options);
                options.MinimumEventLevel = LogLevel.Warning;
            })
            .ClearLoggerProviders()
            .LoggingAddSentry();
        using var host = testHost.Build(kind);

        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Worker").LogWarning("one logged warning");

        _events.Should().ContainSingle();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_LoggerProvidersCleared_WarnsLoggerProvidersRemoved(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(CaptureDiagnostics)
            .ClearLoggerProviders();

        using var host = testHost.Build(kind);

        LoggerProvidersRemovedWarnings().Should().ContainSingle();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_LoggerProvidersClearedThenLoggingAddSentry_DoesNotWarn(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(CaptureDiagnostics)
            .ClearLoggerProviders()
            .LoggingAddSentry();

        using var host = testHost.Build(kind);

        LoggerProvidersRemovedWarnings().Should().BeEmpty();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_LoggerProvidersKept_DoesNotWarn(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(CaptureDiagnostics);

        using var host = testHost.Build(kind);

        LoggerProvidersRemovedWarnings().Should().BeEmpty();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_LoggerFactoryReplaced_DoesNotCreateLoggerProviders(BuilderKind kind)
    {
        var loggerProviderCreated = false;
        var testHost = new TestHost()
            .UseSentry(CaptureDiagnostics)
            .ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(_ =>
                {
                    loggerProviderCreated = true;
                    return NullLoggerProvider.Instance;
                });
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(NullLoggerFactory.Instance));
            });

        using var host = testHost.Build(kind);

        loggerProviderCreated.Should().BeFalse();
        LoggerProvidersRemovedWarnings().Should().BeEmpty();
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_CapturedEvent_ReportsHostingSdk(BuilderKind kind)
    {
        using var host = new TestHost().UseSentry(Capture).Build(kind);

        LogError(host, "one logged error");

        var sdk = _events.Should().ContainSingle().Which.Sdk;
        sdk.Name.Should().Be("sentry.dotnet.extensions.hosting");
        sdk.Packages.Should().Contain(package => package.Name == "nuget:Sentry.Extensions.Hosting");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_LoggerProvidersClearedAfterwards_CapturesBeforeHostStarts(BuilderKind kind)
    {
        var testHost = new TestHost().UseSentry(Capture).ClearLoggerProviders();
        using var host = testHost.Build(kind);

        SentrySdk.CaptureMessage("captured before the host starts");

        _events.Should().ContainSingle(e => e.Message!.Message == "captured before the host starts");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public void UseSentry_LoggerFactoryReplaced_CapturesBeforeHostStarts(BuilderKind kind)
    {
        var testHost = new TestHost()
            .UseSentry(Capture)
            .ConfigureServices(services => services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance));
        using var host = testHost.Build(kind);

        SentrySdk.CaptureMessage("captured before the host starts");

        _events.Should().ContainSingle(e => e.Message!.Message == "captured before the host starts");
    }

    [Theory, MemberData(nameof(TestHost.Builders), MemberType = typeof(TestHost))]
    public async Task UseSentry_EmptyDsnInJson_StartsHostWithSentryDisabled(BuilderKind kind)
    {
        var testHost = new TestHost { Json = """{ "Sentry": { "Dsn": "" } }""" }.UseSentry(DoNotSend);
        using var host = testHost.Build(kind);

        await host.StartAsync();

        SentrySdk.IsEnabled.Should().BeFalse();
        ResolveOptions(host).Dsn.Should().BeEmpty();
        await host.StopAsync();
    }
}
