namespace Sentry.Profiling.Tests;

#nullable enable

public class ProfilingSentryOptionsExtensionsTests
{
    private readonly InMemoryDiagnosticLogger _logger = new();
    private readonly SentryOptions _options = new()
    {
        Dsn = ValidDsn,
        AutoSessionTracking = false,
        IsGlobalModeEnabled = true,
        BackgroundWorker = Substitute.For<IBackgroundWorker>(),
        Debug = true,

        // Set explicitly for this test in case the defaults change in the future.
        TracesSampleRate = 0.0,
        TracesSampler = null
    };

    public ProfilingSentryOptionsExtensionsTests()
    {
        _options.DiagnosticLogger = _logger;
        _options.AddProfilingIntegration();
    }

    private Hub GetSut() => new(_options, Substitute.For<ISentryClient>());

    private static IEnumerable<ISdkIntegration> GetIntegrations(ISentryClient hub) =>
        hub.GetSentryOptions()?.Integrations ?? Enumerable.Empty<ISdkIntegration>();

    [Fact]
    public void Integration_DisabledWithDefaultOptions()
    {
        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.Contains(_logger.Entries, x => x.Message == "Profiling Integration is disabled because profiling is disabled by configuration."
                                                     && x.Level == SentryLevel.Info);
    }

    [Fact]
    public void Integration_EnabledBySampleRate()
    {
        _options.TracesSampleRate = 1.0;
        _options.ProfilesSampleRate = 1.0;

        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.Contains(integrations, i => i is ProfilingIntegration);
    }

    [Fact]
    public void DisableProfilingIntegration_RemovesProfilingIntegration()
    {
        _options.TracesSampleRate = 1.0;
        _options.ProfilesSampleRate = 1.0;
        _options.DisableProfilingIntegration();

        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.DoesNotContain(integrations, i => i is ProfilingIntegration);
    }

    [Fact]
    public void HubDispose_DisposesTheProfilerFactoryItCreated()
    {
        _options.TracesSampleRate = 1.0;
        _options.ProfilesSampleRate = 1.0;

        var hub = GetSut();
        var factory = (SamplingTransactionProfilerFactory)_options.TransactionProfilerFactory!;
        Assert.False(factory.IsDisposed);

        hub.Dispose();

        // ProfilingIntegration must be IDisposable for the Hub to register it for cleanup - otherwise
        // the factory (and the EventPipe session it owns) is never disposed on SDK shutdown.
        Assert.True(factory.IsDisposed);
    }

    [Fact]
    public void HubDispose_DoesNotDisposeAProfilerFactoryItDidNotCreate()
    {
        _options.TracesSampleRate = 1.0;
        _options.ProfilesSampleRate = 1.0;

        var externalFactory = Substitute.For<ITransactionProfilerFactory, IDisposable>();
        _options.TransactionProfilerFactory = externalFactory;

        using (var hub = GetSut())
        {
            Assert.Same(externalFactory, _options.TransactionProfilerFactory);
        }

        ((IDisposable)externalFactory).DidNotReceive().Dispose();
    }

    [Fact]
    public void AddProfilingIntegration_DoesntDuplicate()
    {
        var options = new SentryOptions();

        options.AddProfilingIntegration();
        options.AddProfilingIntegration();

        Assert.Single(options.Integrations, x => x is ProfilingIntegration);
    }
}
