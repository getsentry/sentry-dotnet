using Sentry.Internal.DiagnosticSource;

namespace Sentry.DiagnosticSource.Tests;

public class DiagnosticsSentryOptionsExtensionsTests
{
    private readonly SentryOptions _options = new()
    {
        Dsn = ValidDsn,
        AutoSessionTracking = false,
        IsGlobalModeEnabled = true,
        BackgroundWorker = Substitute.For<IBackgroundWorker>(),

        // Set explicitly for this test in case the defaults change in the future.
        TracesSampleRate = 0.0,
        TracesSampler = null
    };

    public DiagnosticsSentryOptionsExtensionsTests()
    {
#if NETFRAMEWORK
        _options.AddDiagnosticSourceIntegration();
#endif
    }

    private Hub GetSut() => new(_options, Substitute.For<ISentryClient>());

    private static IEnumerable<ISdkIntegration> GetIntegrations(ISentryClient hub) =>
        hub.GetSentryOptions()?.Integrations.Values.Select(x => x.Value) ?? Enumerable.Empty<ISdkIntegration>();

    [Fact]
    public void DiagnosticListenerIntegration_DisabledWithoutTracesSampling()
    {
        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.DoesNotContain(integrations, _ => _ is SentryDiagnosticListenerIntegration);
    }

    [Fact]
    public void DiagnosticListenerIntegration_EnabledWithTracesSampleRate()
    {
        _options.TracesSampleRate = 1.0;

        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.Contains(integrations, _ => _ is SentryDiagnosticListenerIntegration);
    }

    [Fact]
    public void DiagnosticListenerIntegration_EnabledWithTracesSampler()
    {
        // It doesn't matter what the sampler returns, just that it exists.
        _options.TracesSampler = _ => null;

        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.Contains(integrations, _ => _ is SentryDiagnosticListenerIntegration);
    }

    [Fact]
    public void DisableDiagnosticListenerIntegration_RemovesDiagnosticSourceIntegration()
    {
        _options.TracesSampleRate = 1.0;
        _options.DisableDiagnosticSourceIntegration();

        using var hub = GetSut();
        var integrations = GetIntegrations(hub);
        Assert.DoesNotContain(integrations, _ => _ is SentryDiagnosticListenerIntegration);
    }

#if NETFRAMEWORK
    [Fact]
    public void DiagnosticListenerIntegration_AddDiagnosticSourceIntegration_DoesntDuplicate()
    {
        var options = new SentryOptions();

        options.AddDiagnosticSourceIntegration();
        options.AddDiagnosticSourceIntegration();

        Assert.Single(options.Integrations!, x => x.Value.Value is SentryDiagnosticListenerIntegration);
    }
#endif
}
