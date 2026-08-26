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

        Assert.True(factory.IsDisposed);
    }

    [Fact]
    public void HubDispose_OptionsReusedByANewHub_ProfilerFactoryIsRecreated()
    {
        _options.TracesSampleRate = 1.0;
        _options.ProfilesSampleRate = 1.0;

        using (var first = GetSut())
        {
            Assert.NotNull(_options.TransactionProfilerFactory);
        }

        using var second = GetSut();

        var factory = (SamplingTransactionProfilerFactory)_options.TransactionProfilerFactory!;
        factory.IsDisposed.Should().BeFalse(
            "a disposed factory left in the options would silently stop profiling for the new hub");
    }

    [Fact]
    public void HubDispose_WhileAnotherHubIsStillRegistered_KeepsTheProfilerFactoryAlive()
    {
        _options.TracesSampleRate = 1.0;
        _options.ProfilesSampleRate = 1.0;

        // SentrySdk.Init is UseHub(InitHub(options)) - the replacement hub registers before the
        // outgoing one is disposed.
        var first = GetSut();
        var factory = (SamplingTransactionProfilerFactory)_options.TransactionProfilerFactory!;
        var second = GetSut();

        first.Dispose();

        _options.TransactionProfilerFactory.Should().BeSameAs(factory);
        factory.IsDisposed.Should().BeFalse("the replacement hub is still using it");

        second.Dispose();

        factory.IsDisposed.Should().BeTrue("the last hub using it has gone");
        _options.TransactionProfilerFactory.Should().BeNull();
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
