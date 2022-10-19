using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class AndroidDiagnosticLoggerTests
{
    private class Fixture
    {
        public IDiagnosticLogger ManagedLogger { get; }

        public Fixture()
        {
            ManagedLogger = Substitute.ForPartsOf<DiagnosticLogger>(SentryLevel.Debug);
        }

        public AndroidDiagnosticLogger GetSut() => new(ManagedLogger);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void IsEnabled()
    {
        var androidLogger = _fixture.GetSut();
        var enabled = androidLogger.IsEnabled(JavaSdk.SentryLevel.Debug);
        Assert.True(enabled);
    }

    [Fact]
    public void Log_Simple()
    {
        var androidLogger = _fixture.GetSut();
        androidLogger.Log(JavaSdk.SentryLevel.Debug, "test", args:null);
    }
}
