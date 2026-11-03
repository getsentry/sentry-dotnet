namespace Sentry.Tests.Extensibility;

public class HubDiagnosticExtensionsTests
{
    private readonly IDiagnosticLogger _logger = Substitute.For<IDiagnosticLogger>();

    [Fact]
    public void GetDiagnosticLogger_DebugEnabled_ReturnsOptionsLogger()
    {
        using var hub = new Hub(new SentryOptions { Dsn = ValidDsn, Debug = true, DiagnosticLogger = _logger });

        hub.GetDiagnosticLogger().Should().BeSameAs(_logger);
    }

    [Fact]
    public void GetDiagnosticLogger_DebugDisabled_ReturnsNull()
    {
        using var hub = new Hub(new SentryOptions { Dsn = ValidDsn, Debug = false, DiagnosticLogger = _logger });

        hub.GetDiagnosticLogger().Should().BeNull();
    }

    [Fact]
    public void GetDiagnosticLogger_CustomHub_ReturnsNull()
    {
        var hub = Substitute.For<IHub>();

        hub.GetDiagnosticLogger().Should().BeNull();
    }
}
